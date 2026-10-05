using System;
using Outbreak.Loadouts;
using Outbreak.Player;
using Outbreak.Raids;
using UnityEngine;

namespace Outbreak.Weapons
{
    // LateUpdate runs after Bodycam (default order 0), so shots use this frame's composed view.
    [DefaultExecutionOrder(100), RequireComponent(typeof(PlayerInput), typeof(PlayerMotor), typeof(PlayerLook))]
    public sealed class FirearmController : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private WeaponDefinition primary;
        [SerializeField] private WeaponDefinition secondary;
        [SerializeField, Min(0)] private int primaryReserve = 180;
        [SerializeField, Min(0)] private int secondaryReserve = 75;
        [SerializeField, Min(0)] private float switchDuration = 0.3f;
        [SerializeField] private WeaponViewModel primaryView;
        [SerializeField] private WeaponViewModel secondaryView;
        [SerializeField] private LayerMask hitMask = ~0;
        [Header("Weapon clearance")]
        [SerializeField, Min(0.01f)] private float wallProbeRadius = 0.055f;
        [SerializeField, Min(0.01f)] private float wallMargin = 0.16f;
        [SerializeField] private AudioSource audioSource;
        private PlayerInput input;
        private PlayerMotor motor;
        private PlayerLook look;
        private IWeaponCameraFeedback cameraFeedback;
        private WeaponSelection selection;
        private float aimWeight;
        private int shotSequence;
        private WeaponRuntimeState outgoingViewState;
        private float obstructionWeight;
        private readonly RaycastHit[] clearanceHits = new RaycastHit[32];
        private readonly Collider[] clearanceOverlaps = new Collider[32];
        public WeaponRuntimeState Current => selection?.Current;
        public bool IsSwitching => selection != null && selection.IsSwitching;
        public float AimWeight => aimWeight;
        public float CurrentSpread { get; private set; }
        public bool IsObstructed { get; private set; }
        public WeaponRuntimeState PrimaryState => selection?.Primary;
        public WeaponRuntimeState SecondaryState => selection?.Secondary;
        public event Action<WeaponRuntimeState> OnWeaponFired;
        public event Action<WeaponRuntimeState> OnDryFire;
        public event Action<WeaponRuntimeState> OnReloadStarted;
        public event Action<WeaponRuntimeState> OnReloadCompleted;
        public event Action<WeaponRuntimeState> OnWeaponEquipped;
        public event Action<WeaponRuntimeState> OnAmmoChanged;
        public event Action<bool> OnAimChanged;
        public event Action<DamageInfo> OnHit;
        public event Action<WeaponShotFeedback> OnShotFeedback;
        public event Action<WeaponImpactFeedback> OnImpact;
        public event Action<WeaponRuntimeState, WeaponAudioCue, AudioClip> OnAudioCue;

        private void Awake()
        {
            input = GetComponent<PlayerInput>(); motor = GetComponent<PlayerMotor>(); look = GetComponent<PlayerLook>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            cameraFeedback = GetComponentInChildren<IWeaponCameraFeedback>();
            if (viewCamera == null) { Debug.LogError("[Gunplay] Missing view camera.", this); enabled = false; return; }
            EquipDefinitions(primary, secondary, primaryReserve, secondaryReserve);
            if (primary == null && secondary == null && (primaryView != null || secondaryView != null))
                Debug.LogWarning("[Gunplay] Viewmodels assigned but no weapon definitions. Assign Primary/Secondary or equip a raid loadout; both views are hidden until equipped.", this);
        }
        private void Start() { if (Current != null) { OnWeaponEquipped?.Invoke(Current); AudioFeedback(Current, WeaponAudioCue.Equip); } }

        // Resolve existing raid items through a firearm catalog; no duplicate item identities or inventory.
        // Ammo remains debug/runtime for 3A; a future raid ammo adapter can supply these initial quantities.
        public void EquipFromRaid(RaidSession raid, WeaponDefinition[] catalog, int primaryAmmo = 0, int secondaryAmmo = 0)
        {
            if (raid == null || !raid.IsActive) throw new ArgumentException("An active raid is required.");
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var equipment = raid.Equipment;
            WeaponDefinition Resolve(LoadoutSlot slot)
            {
                var item = equipment.Get(slot)?.Item;
                if (item == null) return null;
                foreach (var definition in catalog)
                    if (definition != null && definition.WeaponId == item.Id) return definition;
                throw new ArgumentException("Missing firearm definition for " + item.Id);
            }
            EquipDefinitions(Resolve(LoadoutSlot.Primary), Resolve(LoadoutSlot.Secondary), primaryAmmo, secondaryAmmo);
            if (Current != null) OnWeaponEquipped?.Invoke(Current);
        }
        private void EquipDefinitions(WeaponDefinition p, WeaponDefinition s, int pAmmo, int sAmmo)
        {
            p?.Validate(); s?.Validate();
            selection?.CancelActions();
            aimWeight = 0;
            shotSequence = 0;
            cameraFeedback?.SetWeaponAim(0, 75);
            var first = p != null ? new WeaponRuntimeState(p, pAmmo) : null;
            var second = s != null ? new WeaponRuntimeState(s, sAmmo) : null;
            selection = new WeaponSelection(first, second, switchDuration);
            outgoingViewState = null; obstructionWeight = 0; IsObstructed = false;
            primaryView?.Bind(first); secondaryView?.Bind(second);
            Bind(first); Bind(second);
            selection.OnWeaponEquipped += state => { shotSequence = 0; outgoingViewState = null; OnWeaponEquipped?.Invoke(state); AudioFeedback(state, WeaponAudioCue.Equip); };
            ShowViews(selection.Current)?.UpdatePose(0, false, false, 0);
        }
        private void Bind(WeaponRuntimeState state)
        {
            if (state == null) return;
            state.OnAmmoChanged += () => OnAmmoChanged?.Invoke(state);
            state.OnDryFire += () => { OnDryFire?.Invoke(state); AudioFeedback(state, WeaponAudioCue.DryFire); };
            state.OnReloadStarted += () => { OnReloadStarted?.Invoke(state); AudioFeedback(state, WeaponAudioCue.Reload); };
            state.OnReloadCompleted += () => OnReloadCompleted?.Invoke(state);
            state.OnAimChanged += value => OnAimChanged?.Invoke(value);
        }
        private void Update()
        {
            if (selection == null) return;
            double now = Time.timeAsDouble;
            selection.Tick(now);
            if (input.PrimaryPressed) SelectWeapon(true, now);
            else if (input.SecondaryPressed) SelectWeapon(false, now);
            var state = Current;
            if (state == null) return;
            state.Tick(now);
            if (input.ReloadPressed && !IsSwitching && !motor.IsMantling) state.StartReload(now);
            if (input.FireModePressed && !IsSwitching) state.ToggleFireMode();
            state.SetAim(input.AimHeld && !IsSwitching && !motor.IsMantling && !motor.IsSprinting && !IsObstructed);
            aimWeight = Mathf.Lerp(aimWeight, state.IsAiming ? 1 : 0, 1 - Mathf.Exp(-state.Stats.AdsSpeed * Time.deltaTime));
            cameraFeedback?.SetWeaponAim(aimWeight, state.Definition.adsFov);
            if (!input.FireHeld) shotSequence = 0;
        }
        private void LateUpdate()
        {
            var state = Current;
            if (state == null) { ShowViews(null); return; }
            float progress = selection.SwitchProgress(Time.timeAsDouble);
            var shownState = IsSwitching && progress < 0.5f && outgoingViewState != null ? outgoingViewState : state;
            var view = ShowViews(shownState);
            ProbeObstruction(view);
            if (IsObstructed) state.SetAim(false);
            float switchLower = !IsSwitching ? 0 : progress < 0.5f ? Mathf.SmoothStep(0, 1, progress * 2) : 1 - Mathf.SmoothStep(0, 1, (progress - 0.5f) * 2);
            float reloadProgress = state.IsReloading ? (float)((Time.timeAsDouble - state.ReloadStartTime) / (state.ReloadEndTime - state.ReloadStartTime)) : 0;
            view?.UpdatePresentation(new WeaponPresentationInput {
                aim = aimWeight, dt = Time.deltaTime, switchLower = switchLower, reloadProgress = reloadProgress,
                reloading = shownState.IsReloading, sprinting = motor.IsSprinting, crouching = motor.IsCrouching,
                obstruction = obstructionWeight, mouseDelta = look.LookDelta,
                localVelocity = transform.InverseTransformDirection(motor.Velocity)
            });
            var definition = state.Definition;
            var stats = state.Stats;
            CurrentSpread = Mathf.Lerp(stats.HipSpread, stats.AdsSpread, aimWeight);
            CurrentSpread *= Mathf.Lerp(1, definition.movementSpreadMultiplier, Mathf.Clamp01(motor.Speed / Mathf.Max(0.1f, motor.WalkSpeed)));
            if (motor.IsCrouching) CurrentSpread *= definition.crouchSpreadMultiplier;
            if (!motor.IsGrounded) CurrentSpread *= definition.airSpreadMultiplier;
            IsObstructed |= MuzzleBlocked(view);
            if (IsObstructed) state.SetAim(false);
            if (state.ProcessTrigger(input.FireHeld && input.HasControl, Time.timeAsDouble,
                motor.IsMantling, motor.IsSprinting, IsSwitching || (view != null && !view.ReadyToFire),
                IsObstructed) != FireResult.Fired) return;
            Shoot(definition, view);
            float progression = definition.weaponClass == WeaponClass.Rifle ? 1 + Mathf.Min(shotSequence, 8) * 0.035f : 1;
            float horizontal = Mathf.Sin(shotSequence * 1.7f + 0.5f) * stats.RecoilHorizontal;
            look.AddAimRecoil(stats.RecoilVertical * progression, horizontal, definition.recoilRecovery);
            cameraFeedback?.AddWeaponKick(definition.cameraKick);
            float recoilScale = definition.recoilVertical > 0 ? stats.RecoilVertical / definition.recoilVertical : 1;
            view?.Kick(definition.visualKick * recoilScale, definition.visualRotation * recoilScale);
            shotSequence++;
            OnWeaponFired?.Invoke(state);
            OnShotFeedback?.Invoke(new WeaponShotFeedback(state, view, viewCamera.transform, aimWeight));
            AudioFeedback(state, WeaponAudioCue.Fire);
        }
        private void SelectWeapon(bool primarySlot, double now)
        {
            var previous = Current;
            if (selection.Select(primarySlot, now)) outgoingViewState = previous;
        }
        private void AudioFeedback(WeaponRuntimeState state, WeaponAudioCue cue)
        {
            var clip = WeaponAudio.Resolve(state, cue);
            OnAudioCue?.Invoke(state, cue, clip);
            WeaponAudio.PlayOptional(audioSource, clip);
        }
        private void ProbeObstruction(WeaponViewModel view)
        {
            obstructionWeight = 0;
            if (view == null) { IsObstructed = false; return; }
            Transform cameraTransform = viewCamera.transform;
            Vector3 neutralTip = view.DesiredMuzzleCameraPosition(aimWeight);
            float distance = neutralTip.magnitude;
            // Centre and muzzle-lane probes catch both a frontal wall and an edge during lean.
            for (int probe = 0; probe < 2; probe++)
            {
                var direction = probe == 0 ? cameraTransform.forward : cameraTransform.TransformDirection(neutralTip.normalized);
                int count = Physics.SphereCastNonAlloc(cameraTransform.position, wallProbeRadius, direction,
                    clearanceHits, distance + wallMargin, hitMask, QueryTriggerInteraction.Ignore);
                if (count == clearanceHits.Length) obstructionWeight = 1; // Fail closed on saturation.
                for (int i = 0; i < count; i++)
                {
                    var hit = clearanceHits[i];
                    if (hit.transform.IsChildOf(transform)) continue;
                    obstructionWeight = Mathf.Max(obstructionWeight, Mathf.InverseLerp(distance + wallMargin, 0.22f, hit.distance));
                }
            }
            if (OverlapsWorld(cameraTransform.position, wallProbeRadius)) obstructionWeight = 1;
            IsObstructed = obstructionWeight > (IsObstructed ? 0.03f : 0.1f);
        }
        private bool MuzzleBlocked(WeaponViewModel view)
        {
            if (view == null || view.Muzzle == null) return false;
            var start = viewCamera.transform.position;
            var delta = view.Muzzle.position - start;
            if (Cast(start, delta.normalized, delta.magnitude + 0.035f, out _)) return true;
            return OverlapsWorld(view.Muzzle.position, 0.025f);
        }
        private bool OverlapsWorld(Vector3 position, float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(position, radius, clearanceOverlaps, hitMask, QueryTriggerInteraction.Ignore);
            if (count == clearanceOverlaps.Length) return true;
            for (int i = 0; i < count; i++) if (!clearanceOverlaps[i].transform.IsChildOf(transform)) return true;
            return false;
        }
        private WeaponViewModel ShowViews(WeaponRuntimeState state)
        {
            bool p = state != null && state == selection?.Primary;
            bool s = state != null && state == selection?.Secondary;
            if (primaryView != null) primaryView.gameObject.SetActive(p);
            if (secondaryView != null) secondaryView.gameObject.SetActive(s);
            return p ? primaryView : s ? secondaryView : null;
        }
        private bool Cast(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearest)
        {
            // RaycastAll intentionally avoids a fixed-size buffer silently missing a nearby obstruction.
            var hits = Physics.RaycastAll(origin, direction, distance, hitMask, QueryTriggerInteraction.Ignore);
            nearest = default;
            float best = float.PositiveInfinity;
            foreach (var hit in hits)
                if (!hit.transform.IsChildOf(transform) && hit.distance < best) { best = hit.distance; nearest = hit; }
            return best < float.PositiveInfinity;
        }
        private void Shoot(WeaponDefinition definition, WeaponViewModel view)
        {
            Transform cameraTransform = viewCamera.transform;
            Vector2 disk = UnityEngine.Random.insideUnitCircle * Mathf.Tan(CurrentSpread * Mathf.Deg2Rad);
            Vector3 direction = (cameraTransform.forward + cameraTransform.right * disk.x + cameraTransform.up * disk.y).normalized;
            Vector3 origin = cameraTransform.position;
            bool cameraHit = Cast(origin, direction, definition.range, out var aimHit);
            Vector3 target = cameraHit ? aimHit.point : origin + direction * definition.range;
            Vector3 muzzle = view != null && view.Muzzle != null ? view.Muzzle.position : origin;
            // If the muzzle has crossed a wall, first clip its path from the camera to that wall.
            Vector3 toMuzzle = muzzle - origin;
            RaycastHit hit = default;
            bool hitSomething = toMuzzle.sqrMagnitude > 0.000001f && Cast(origin, toMuzzle.normalized, toMuzzle.magnitude, out hit);
            if (!hitSomething)
            {
                Vector3 toTarget = target - muzzle;
                view?.PointMuzzleAt(target);
                hitSomething = Cast(muzzle, toTarget.normalized, Mathf.Min(definition.range, toTarget.magnitude + 0.015f), out hit);
            }
            if (!hitSomething) return;
            var hitbox = hit.collider.GetComponent<DamageHitbox>();
            var receiver = hitbox != null ? hitbox.Receiver : hit.collider.GetComponentInParent<IDamageable>();
            OnImpact?.Invoke(new WeaponImpactFeedback(hit.point, hit.normal, hitbox != null || receiver != null));
            if (receiver == null) return;
            var damage = new DamageInfo(definition, hitbox != null ? hitbox.zone : HitZone.Body, hit.point, hit.normal, gameObject);
            receiver.ApplyDamage(damage);
            OnHit?.Invoke(damage);
        }
        private void OnDisable()
        {
            selection?.CancelActions();
            aimWeight = 0;
            cameraFeedback?.SetWeaponAim(0, 75);
            if (primaryView != null) primaryView.gameObject.SetActive(false);
            if (secondaryView != null) secondaryView.gameObject.SetActive(false);
        }
    }
}
