using UnityEngine;

namespace Outbreak.Weapons
{
    public struct WeaponPresentationInput
    {
        public float aim, dt, switchLower, reloadProgress, obstruction;
        public bool reloading, sprinting, crouching;
        public Vector2 mouseDelta;
        public Vector3 localVelocity;
    }

    public sealed class WeaponViewModel : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform ejectionPoint;
        [SerializeField] private Transform opticSocket, muzzleSocket, magazineSocket, gripSocket;
        [SerializeField] private GameObject baseMagazine;
        [SerializeField] private Vector3 hipPosition = new Vector3(0.16f, -0.14f, 0.46f);
        [SerializeField] private Vector3 adsPosition = new Vector3(0, -0.055f, 0.46f);
        [Header("Viewmodel inertia only; never modifies camera or aim")]
        [SerializeField, Min(0)] private float mouseSway = 0.004f;
        [SerializeField, Min(0)] private float strafeSway = 0.35f;
        [SerializeField, Min(0)] private float accelerationSway = 0.035f;
        [SerializeField, Min(0)] private float maximumSway = 1.5f;
        [SerializeField, Min(0.1f)] private float swayResponse = 12;
        [SerializeField, Range(0, 1)] private float adsSwayScale = 0.06f;
        [SerializeField, Range(0, 1)] private float crouchSwayScale = 0.8f;
        [SerializeField, Min(1)] private float sprintSwayScale = 1.3f;
        [SerializeField, Min(0.1f)] private float readyResponse = 12;
        private float kick, rotation, actionLower, actionTilt, actionRoll, lowReady, wall;
        private Vector3 sway, previousVelocity, currentAds;
        private bool adsInitialized;
        private WeaponRuntimeState boundState;
        private readonly GameObject[] instances = new GameObject[4];
        private Transform opticAnchor, extendedMuzzle;
        public Transform Muzzle => extendedMuzzle != null ? extendedMuzzle : muzzle;
        public Transform EjectionPoint => ejectionPoint != null ? ejectionPoint : transform;
        public bool ReadyToFire => lowReady < 0.08f;

        public void Bind(WeaponRuntimeState state)
        {
            if (boundState == state) return;
            if (boundState != null) boundState.OnAttachmentsChanged -= RefreshVisuals;
            boundState = state;
            if (boundState != null) boundState.OnAttachmentsChanged += RefreshVisuals;
            RefreshVisuals();
        }
        private void RefreshVisuals()
        {
            opticAnchor = extendedMuzzle = null;
            for (int i = 0; i < instances.Length; i++)
            {
                if (instances[i] != null)
                {
                    instances[i].SetActive(false);
                    if (Application.isPlaying) Destroy(instances[i]); else DestroyImmediate(instances[i]);
                    instances[i] = null;
                }
                var slot = (AttachmentSlot)i;
                var attachment = boundState?.Attachments[slot];
                var socket = Socket(slot);
                if (attachment == null || attachment.visualPrefab == null || socket == null) continue;
                instances[i] = Instantiate(attachment.visualPrefab, socket, false);
                var anchors = instances[i].GetComponent<AttachmentVisual>();
                if (anchors == null) continue;
                if (slot == AttachmentSlot.Optic) opticAnchor = anchors.adsAnchor;
                if (slot == AttachmentSlot.Muzzle) extendedMuzzle = anchors.muzzleTip;
            }
            if (baseMagazine != null) baseMagazine.SetActive(instances[(int)AttachmentSlot.Magazine] == null);
        }
        private Transform Socket(AttachmentSlot slot)
        {
            switch (slot)
            {
                case AttachmentSlot.Optic: return opticSocket;
                case AttachmentSlot.Muzzle: return muzzleSocket;
                case AttachmentSlot.Magazine: return magazineSocket;
                default: return gripSocket;
            }
        }
        private Vector3 TargetAds()
        {
            if (opticAnchor == null) return adsPosition;
            var sight = transform.InverseTransformPoint(opticAnchor.position);
            return new Vector3(-sight.x, -sight.y, adsPosition.z);
        }
        // Probe the unlowered pose, avoiding retract -> clear -> extend oscillation.
        public Vector3 DesiredMuzzleCameraPosition(float aim)
        {
            Vector3 localTip = Muzzle != null ? transform.InverseTransformPoint(Muzzle.position) : Vector3.forward * 0.3f;
            return transform.parent.localPosition + Vector3.Lerp(hipPosition, TargetAds(), aim) + localTip;
        }
        public void Kick(float distance, float degrees)
        { kick = Mathf.Min(0.10f, kick + distance); rotation = Mathf.Min(14, rotation + degrees); }

        // Legacy entry point retained for 3A/editor setup.
        public void UpdatePose(float aim, bool switching, bool reloading, float dt)
        { UpdatePresentation(new WeaponPresentationInput { aim = aim, switchLower = switching ? 1 : 0, reloading = reloading, dt = dt }); }

        public void UpdatePresentation(WeaponPresentationInput input)
        {
            float dt = Mathf.Max(0, input.dt);
            float response = 1 - Mathf.Exp(-18 * dt);
            kick = Mathf.Lerp(kick, 0, 1 - Mathf.Exp(-16 * dt));
            rotation = Mathf.Lerp(rotation, 0, response);
            if (!adsInitialized) { currentAds = adsPosition; adsInitialized = true; }
            currentAds = Vector3.Lerp(currentAds, TargetAds(), response);
            var acceleration = dt > 0 ? Vector3.ClampMagnitude((input.localVelocity - previousVelocity) / dt, 20) : Vector3.zero;
            previousVelocity = input.localVelocity;
            var mouseRate = dt > 0 ? input.mouseDelta / dt : Vector2.zero;
            var targetSway = new Vector3(mouseRate.y * mouseSway + acceleration.z * accelerationSway,
                -mouseRate.x * mouseSway, -input.localVelocity.x * strafeSway - acceleration.x * accelerationSway);
            targetSway = Vector3.ClampMagnitude(targetSway, maximumSway);
            float scale = Mathf.Lerp(1, adsSwayScale, input.aim) * (input.crouching ? crouchSwayScale : 1) * (input.sprinting ? sprintSwayScale : 1);
            sway = Vector3.Lerp(sway, targetSway * scale, 1 - Mathf.Exp(-swayResponse * dt));
            lowReady = Mathf.Lerp(lowReady, input.sprinting ? 1 : 0, 1 - Mathf.Exp(-readyResponse * dt));
            wall = Mathf.Lerp(wall, input.obstruction, 1 - Mathf.Exp(-(input.obstruction > wall ? 30 : 10) * dt));
            float cycle = input.reloading ? Mathf.Sin(Mathf.Clamp01(input.reloadProgress) * Mathf.PI) : 0;
            actionLower = Mathf.Lerp(actionLower, input.reloading ? -0.055f - cycle * 0.025f : 0, response);
            actionTilt = Mathf.Lerp(actionTilt, input.reloading ? 12 + cycle * 6 : 0, response);
            actionRoll = Mathf.Lerp(actionRoll, input.reloading ? -12 - cycle * 7 : 0, response);
            var position = Vector3.Lerp(hipPosition, currentAds, input.aim) +
                new Vector3(-sway.y * 0.002f, actionLower - input.switchLower * 0.24f - lowReady * 0.09f - wall * 0.16f,
                    -kick * (1 - wall * 0.8f) - wall * 0.13f);
            var angles = new Vector3(-rotation + actionTilt + lowReady * 28 + wall * 60, lowReady * -12, actionRoll + lowReady * -8) + sway;
            // Do not stack full reload + sprint + avoidance rotations through the camera.
            angles.x = Mathf.Clamp(angles.x, -16, 68);
            transform.localPosition = position;
            transform.localRotation = Quaternion.Euler(angles);
        }
        public void PointMuzzleAt(Vector3 target)
        { if (Muzzle != null && (target - Muzzle.position).sqrMagnitude > 0.000001f) Muzzle.rotation = Quaternion.LookRotation(target - Muzzle.position); }
        private void OnDisable()
        { kick = rotation = actionTilt = actionRoll = actionLower = lowReady = wall = 0; sway = previousVelocity = Vector3.zero; }
        private void OnDestroy() { if (boundState != null) boundState.OnAttachmentsChanged -= RefreshVisuals; }
    }
}
