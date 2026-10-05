using System;
using Outbreak.Player;
using Outbreak.Weapons;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Outbreak.Cameras
{
    [DisallowMultipleComponent]
    public sealed class BodycamController : MonoBehaviour, IPlayerLeanState, IWeaponCameraFeedback
    {
        [Header("References: attach to BodycamRig")]
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerLook look;
        [SerializeField] private PlayerInput input;
        [SerializeField] private UnityEngine.Camera viewCamera;
        [Header("Comfort")]
        [SerializeField, Range(0, 1)] private float effectStrength = 0.65f;
        [SerializeField, Min(0.1f)] private float positionResponse = 14;
        [SerializeField, Min(0.1f)] private float rotationResponse = 16;
        [Header("Idle breathing")]
        [SerializeField, Min(0)] private float breathingAmplitude = 0.0015f;
        [SerializeField, Min(0)] private float breathingFrequency = 0.22f;
        [Header("Walk / sprint bob")]
        [SerializeField, Min(0)] private float walkBobHeight = 0.0138f;
        [SerializeField, Min(0)] private float walkBobSide = 0.01035f;
        [SerializeField, Min(0)] private float sprintBobHeight = 0.0253f;
        [SerializeField, Min(0)] private float sprintBobSide = 0.0161f;
        [SerializeField, Min(0.1f)] private float strideLength = 1.7f;
        [SerializeField, Range(0, 0.5f)] private float sprintIrregularity = 0.12f;
        [Header("Sway / tilt (degrees)")]
        [SerializeField, Min(0)] private float mouseSway = 0.006f;
        [SerializeField, Range(0, 3)] private float maximumSway = 0.8f;
        [SerializeField, Range(0, 4)] private float strafeRoll = 1.3f;
        [SerializeField, Min(0.01f)] private float swaySmoothTime = 0.085f;
        [SerializeField, Min(0)] private float accelerationTilt = 0.055f;
        [SerializeField, Range(0, 4)] private float maximumAccelerationTilt = 1.2f;
        [Header("Tactical lean")]
        [SerializeField, Range(0, 12)] private float leanAngle = 7;
        [SerializeField, Range(0, 0.3f)] private float leanDistance = 0.18f;
        [SerializeField, Min(0.1f)] private float leanSpeed = 10;
        [SerializeField, Min(0.02f)] private float leanCollisionRadius = 0.12f;
        [SerializeField] private LayerMask leanCollisionMask = ~0;
        [Header("Landing")]
        [SerializeField, Min(0)] private float minimumImpactSpeed = 4;
        [SerializeField, Min(0.1f)] private float fullImpactSpeed = 13;
        [SerializeField, Range(0, 0.15f)] private float landingDrop = 0.055f;
        [SerializeField, Range(0, 6)] private float landingPitch = 2.2f;
        [SerializeField, Range(0, 3)] private float landingRoll = 0.65f;
        [SerializeField, Min(0.01f)] private float landingRecovery = 0.18f;
        [Header("Lens / FOV (vertical degrees)")]
        [SerializeField, Range(50, 95)] private float normalFov = 75;
        [SerializeField, Range(0, 12)] private float sprintFovIncrease = 4;
        [SerializeField, Min(0.1f)] private float fovResponse = 7;
        [Tooltip("Optional dedicated Volume; do not assign a shared environment Volume.")]
        [SerializeField] private Volume lensVolume;
        [SerializeField] private bool enableLensDistortion = true;
        [SerializeField, Range(-0.2f, 0.2f)] private float lensDistortion = -0.06f;
        private VolumeProfile runtimeLensProfile;
        private VolumeProfile originalLensProfile;
        private LensDistortion distortion;
        private Vector3 basePosition, positionOffset, rotationOffset, previousVelocity;
        private Quaternion baseRotation;
        private float phase, landing, landingVelocity, landingSign = 1;
        private bool initialized;
        private Vector2 sway, swayVelocity;
        private float requestedLean;
        private float weaponAim, weaponFov = 58, weaponKick;
        public void SetWeaponAim(float weight, float fov)
        { weaponAim = Mathf.Clamp01(weight); weaponFov = Mathf.Clamp(fov, 30, 95); }
        public void AddWeaponKick(float degrees) => weaponKick = Mathf.Min(2, weaponKick + Mathf.Max(0, degrees));
        private readonly RaycastHit[] leanHits = new RaycastHit[24];
        private readonly Collider[] leanOverlaps = new Collider[24];
        public float LeanAmount { get; private set; }
        public bool IsLeaning => Mathf.Abs(LeanAmount) > 0.01f;
        public bool IsInDeathState { get; private set; }
        public event Action OnDeathStateEntered;

        private void Awake()
        {
            if (motor == null || look == null || input == null || viewCamera == null)
            {
                Debug.LogError("[OUTBREAK FPS] Bodycam needs Motor, Look, Input and Camera references.", this);
                enabled = false;
                return;
            }
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
            viewCamera.fieldOfView = normalFov;
            initialized = true;
            if (lensVolume != null)
            {
                originalLensProfile = lensVolume.sharedProfile;
                runtimeLensProfile = originalLensProfile != null ? Instantiate(originalLensProfile) : ScriptableObject.CreateInstance<VolumeProfile>();
                // Clone components too: VolumeProfile cloning alone shares its overrides.
                if (originalLensProfile != null)
                {
                    runtimeLensProfile.components.Clear();
                    foreach (var component in originalLensProfile.components) runtimeLensProfile.components.Add(Instantiate(component));
                }
                if (!runtimeLensProfile.TryGet(out distortion)) distortion = runtimeLensProfile.Add<LensDistortion>();
                lensVolume.sharedProfile = runtimeLensProfile;
            }
        }
        private void OnEnable()
        {
            if (motor != null) motor.OnLand += HandleLand;
        }
        private void OnDisable()
        {
            if (motor != null) motor.OnLand -= HandleLand;
            if (!initialized || IsInDeathState) return;
            transform.localPosition = basePosition;
            transform.localRotation = baseRotation;
            if (viewCamera != null) viewCamera.fieldOfView = normalFov;
            if (distortion != null) distortion.active = false;
            positionOffset = rotationOffset = previousVelocity = Vector3.zero;
            landing = landingVelocity = 0;
            weaponAim = weaponKick = 0;
            requestedLean = LeanAmount = 0;
            sway = swayVelocity = Vector2.zero;
        }
        private void LateUpdate()
        {
            if (!initialized || IsInDeathState) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            float referenceSpeed = Mathf.Max(0.1f, motor.WalkSpeed);
            float move = motor.IsGrounded && !motor.IsMantling ? Mathf.Clamp01(motor.Speed / referenceSpeed) : 0;
            bool sprint = motor.IsSprinting;
            phase = (phase + motor.Speed * dt / strideLength * Mathf.PI * 2) % (Mathf.PI * 2);
            float organic = Mathf.Sin(Time.time * 1.37f) * Mathf.Sin(Time.time * 2.11f);
            float irregular = sprint ? 1 + organic * sprintIrregularity : 1;
            float stanceScale = (motor.IsCrouching ? 0.55f : 1) * Mathf.Lerp(1, 0.65f, weaponAim);
            var targetPosition = new Vector3(
                Mathf.Sin(phase) * (sprint ? sprintBobSide : walkBobSide) * move,
                Mathf.Cos(phase * 2) * (sprint ? sprintBobHeight : walkBobHeight) * move * irregular,
                0) * stanceScale;
            targetPosition.y += Mathf.Sin(Time.time * breathingFrequency * Mathf.PI * 2) * breathingAmplitude;
            targetPosition.x += organic * breathingAmplitude * 0.3f;

            Vector3 worldAcceleration = (motor.Velocity - previousVelocity) / Mathf.Max(dt, 0.001f);
            previousVelocity = motor.Velocity;
            float forwardAcceleration = Vector3.Dot(worldAcceleration, motor.transform.forward);
            Vector2 mouseRate = input.HasControl ? look.LookDelta / Mathf.Max(dt, 0.001f) : Vector2.zero;
            float swayYaw = Mathf.Clamp(-mouseRate.x * mouseSway, -maximumSway, maximumSway);
            float swayPitch = Mathf.Clamp(-mouseRate.y * mouseSway, -maximumSway, maximumSway);
            sway = Vector2.SmoothDamp(sway, new Vector2(swayPitch, swayYaw), ref swayVelocity,
                swaySmoothTime, Mathf.Infinity, dt);
            float lateralSpeed = Vector3.Dot(motor.Velocity, motor.transform.right);
            Vector3 targetRotation = new Vector3(
                sway.x * Mathf.Lerp(1, 0.4f, weaponAim) + Mathf.Clamp(forwardAcceleration * accelerationTilt, -maximumAccelerationTilt, maximumAccelerationTilt),
                sway.y * Mathf.Lerp(1, 0.4f, weaponAim),
                -Mathf.Clamp(lateralSpeed / referenceSpeed, -1, 1) * strafeRoll);
            landing = Mathf.SmoothDamp(landing, 0, ref landingVelocity, landingRecovery, Mathf.Infinity, dt);
            targetPosition.y -= landing * landingDrop;
            targetRotation.x += landing * landingPitch;
            targetRotation.z += landing * landingRoll * landingSign;
            targetRotation.x -= weaponKick;
            weaponKick *= Mathf.Exp(-18 * dt);
            positionOffset = Vector3.Lerp(positionOffset, targetPosition * effectStrength, 1 - Mathf.Exp(-positionResponse * dt));
            rotationOffset = Vector3.Lerp(rotationOffset, targetRotation * effectStrength, 1 - Mathf.Exp(-rotationResponse * dt));
            float leanTarget = input.HasControl && !motor.IsMantling ? input.LeanInput : 0;
            requestedLean = Mathf.Lerp(requestedLean, leanTarget, 1 - Mathf.Exp(-leanSpeed * dt));
            Vector3 combinedOffset = positionOffset + Vector3.right * (requestedLean * leanDistance);
            float clearance = CameraTravelFraction(combinedOffset);
            // Collision constrains the final composed translation, including bob, immediately.
            LeanAmount = requestedLean * clearance;
            transform.localPosition = basePosition + combinedOffset * clearance;
            transform.localRotation = baseRotation * Quaternion.Euler(rotationOffset + Vector3.forward * (-LeanAmount * leanAngle));
            float fov = Mathf.Lerp(normalFov + (sprint ? sprintFovIncrease : 0), weaponFov, weaponAim);
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, fov, 1 - Mathf.Exp(-fovResponse * dt));
            if (distortion != null)
            {
                distortion.active = enableLensDistortion;
                distortion.intensity.Override(lensDistortion);
                distortion.scale.Override(1.02f);
            }
        }
        private float CameraTravelFraction(Vector3 localOffset)
        {
            Transform parent = transform.parent;
            if (parent == null) return 0;
            Vector3 origin = parent.TransformPoint(basePosition);
            Vector3 travel = parent.TransformVector(localOffset);
            // Enclose the near plane even for a wider aspect ratio/FOV. The generated camera is
            // centred on BodycamRig; include a custom local camera offset conservatively as well.
            float halfHeight = viewCamera.nearClipPlane * Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float nearRadius = Mathf.Sqrt(viewCamera.nearClipPlane * viewCamera.nearClipPlane +
                halfHeight * halfHeight * (1 + viewCamera.aspect * viewCamera.aspect));
            float radius = Mathf.Max(leanCollisionRadius, nearRadius) + viewCamera.transform.localPosition.magnitude;
            int overlaps = Physics.OverlapSphereNonAlloc(origin, radius, leanOverlaps,
                leanCollisionMask, QueryTriggerInteraction.Ignore);
            if (overlaps == leanOverlaps.Length) return 0;
            for (int i = 0; i < overlaps; i++)
                if (!leanOverlaps[i].transform.IsChildOf(motor.transform)) return 0;
            float distance = travel.magnitude;
            if (distance < 0.00001f) return 1;
            int count = Physics.SphereCastNonAlloc(origin, radius, travel / distance, leanHits,
                distance + 0.01f, leanCollisionMask, QueryTriggerInteraction.Ignore);
            if (count == leanHits.Length) return 0;
            float allowed = distance;
            for (int i = 0; i < count; i++)
                if (!leanHits[i].transform.IsChildOf(motor.transform))
                    allowed = Mathf.Min(allowed, Mathf.Max(0, leanHits[i].distance - 0.01f));
            return Mathf.Clamp01(allowed / distance);
        }
        private void HandleLand(float fallSpeed)
        {
            if (IsInDeathState || fallSpeed <= minimumImpactSpeed) return;
            landing = Mathf.Max(landing, Mathf.InverseLerp(minimumImpactSpeed, Mathf.Max(minimumImpactSpeed + 0.1f, fullImpactSpeed), fallSpeed));
            landingVelocity = 0;
            landingSign = -landingSign;
        }

        // A future death director owns the world-space pose after this handoff.
        public void EnterDeathState()
        {
            if (!initialized || IsInDeathState) return;
            IsInDeathState = true;
            input.SetControlEnabled(false);
            motor.SetMotorEnabled(false);
            look.enabled = false;
            OnDeathStateEntered?.Invoke();
        }
        public void EnterDeathState(Vector3 worldPosition, Quaternion worldRotation)
        {
            EnterDeathState();
            SetDeathPose(worldPosition, worldRotation);
        }
        public void SetDeathPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (!IsInDeathState) return;
            transform.SetPositionAndRotation(worldPosition, worldRotation);
        }
        private void OnDestroy()
        {
            if (runtimeLensProfile == null) return;
            if (lensVolume != null) lensVolume.sharedProfile = originalLensProfile;
            foreach (var component in runtimeLensProfile.components) Destroy(component);
            Destroy(runtimeLensProfile);
        }
    }
}
