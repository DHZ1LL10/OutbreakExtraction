using System;
using UnityEngine;

namespace Outbreak.Player
{
    [DefaultExecutionOrder(-100), RequireComponent(typeof(CharacterController), typeof(PlayerInput)), DisallowMultipleComponent]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("Movement (metres / seconds)")]
        [SerializeField, Min(0)] private float walkSpeed = 3.2f;
        [SerializeField, Min(0)] private float sprintSpeed = 5.4f;
        [SerializeField, Min(0)] private float crouchSpeed = 1.6f;
        [SerializeField, Min(0)] private float slowWalkSpeed = 1.4f;
        [SerializeField, Min(0)] private float crouchSlowWalkSpeed = 0.75f;
        [SerializeField, Min(0.1f)] private float acceleration = 22;
        [SerializeField, Min(0.1f)] private float deceleration = 28;
        [SerializeField, Min(0)] private float airAcceleration = 3;
        [SerializeField, Range(0, 1)] private float sprintForwardThreshold = 0.6f;
        [Header("Gravity / jump")]
        [SerializeField, Min(0.1f)] private float gravity = 24;
        [SerializeField, Min(0)] private float jumpHeight = 1.0f;
        [SerializeField, Min(1)] private float terminalFallSpeed = 35;
        [SerializeField, Min(0)] private float jumpCooldown = 0.3f;
        [SerializeField, Min(0)] private float minimumGroundTimeBeforeJump = 0.08f;
        [SerializeField, Min(0)] private float groundSnapDistance = 0.22f;
        [SerializeField, Min(0)] private float groundStickSpeed = 3;
        [SerializeField] private LayerMask collisionMask = ~0;
        [Header("Stance (root is at feet; unit scale)")]
        [SerializeField] private Transform viewRoot;
        [SerializeField, Min(1)] private float standingHeight = 1.8f;
        [SerializeField, Min(0.6f)] private float crouchingHeight = 1.15f;
        [SerializeField, Min(0.1f)] private float heightChangeSpeed = 4.2f;
        [SerializeField, Min(0)] private float eyeInset = 0.14f;
        [SerializeField, Min(0.01f)] private float standingClearance = 0.03f;
        [Header("Contextual low mantle")]
        [SerializeField, Min(0.05f)] private float minimumMantleHeight = 0.12f;
        [SerializeField, Min(0.1f)] private float maximumMantleHeight = 1.05f;
        [SerializeField, Min(0.1f)] private float mantleDuration = 0.48f;
        [SerializeField, Min(0.05f)] private float mantleForwardDistance = 0.45f;
        [SerializeField, Min(0.05f)] private float mantleDetectionDistance = 0.65f;
        private readonly RaycastHit[] groundHits = new RaycastHit[24];
        private readonly Collider[] overlaps = new Collider[24];
        private CharacterController controller;
        private PlayerInput input;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float groundedTime;
        private float lastJumpTime = float.NegativeInfinity;
        private float defaultStepOffset;
        private bool motorEnabled = true;
        private float airSpeedLimit;
        private Vector3 mantleStart, mantleLift, mantleAcross, mantleTarget;
        private float mantleElapsed;
        public Vector3 Velocity { get; private set; }
        public float Speed => new Vector2(Velocity.x, Velocity.z).magnitude;
        public float WalkSpeed => walkSpeed;
        public float VerticalVelocity => verticalVelocity;
        public bool IsGrounded { get; private set; }
        public bool IsMoving => Speed > 0.1f;
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsWalkingSlow { get; private set; }
        public bool IsMantling { get; private set; }
        public Vector2 MoveInput => input != null ? input.MoveInput : Vector2.zero;
        public Vector2 LookInput => input != null ? input.LookInput : Vector2.zero;
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public float CurrentHeight => controller != null ? controller.height : standingHeight;
        public event Action OnJump;
        public event Action<float> OnLand;
        public event Action OnStartedSprinting;
        public event Action OnStoppedSprinting;
        public event Action<bool> OnCrouchChanged;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInput>();
            ValidateSettings();
            airSpeedLimit = walkSpeed;
            controller.minMoveDistance = 0;
            defaultStepOffset = controller.stepOffset;
            controller.height = standingHeight;
            controller.center = Vector3.up * standingHeight * 0.5f;
            UpdateEyeHeight();
        }
        private void Update()
        {
            Tick(Mathf.Min(Time.deltaTime, 0.05f));
        }

        private void Tick(float dt)
        {
            if (!motorEnabled || !controller.enabled || dt <= 0) return;
            if (IsMantling) { UpdateMantle(dt); return; }
            UpdateStance(dt);
            bool wasGrounded = IsGrounded;
            float incomingFallSpeed = Mathf.Max(0, -verticalVelocity);
            bool supported = ProbeGround(out var support) && verticalVelocity <= 0 &&
                (wasGrounded || support.distance <= 0.08f + controller.skinWidth + 0.02f);
            GroundNormal = supported ? support.normal : Vector3.up;
            groundedTime = supported ? groundedTime + dt : 0;
            bool sprint = supported && input.SprintHeld && !IsCrouching && MoveInput.y >= sprintForwardThreshold;
            IsWalkingSlow = supported && !sprint && input.SlowWalkHeld && MoveInput.sqrMagnitude > 0;
            float speed = IsCrouching ? (IsWalkingSlow ? crouchSlowWalkSpeed : crouchSpeed) :
                sprint ? sprintSpeed : IsWalkingSlow ? slowWalkSpeed : walkSpeed;
            Vector3 desired = (transform.right * MoveInput.x + transform.forward * MoveInput.y) * speed;
            if (supported)
            {
                float rate = MoveInput.sqrMagnitude > 0 ? acceleration : deceleration;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, rate * dt);
                verticalVelocity = -groundStickSpeed;
                airSpeedLimit = Mathf.Max(walkSpeed, horizontalVelocity.magnitude);
            }
            else
            {
                // Air steering cannot add speed beyond the launch speed or normal walking speed.
                // No input preserves momentum. Sprint launches retain their speed while steering;
                // holding/releasing sprint in the air cannot inject extra horizontal energy.
                if (MoveInput.sqrMagnitude > 0)
                    horizontalVelocity = Vector3.ClampMagnitude(Vector3.MoveTowards(horizontalVelocity,
                        desired.normalized * Mathf.Max(speed, horizontalVelocity.magnitude), airAcceleration * dt), airSpeedLimit);
                verticalVelocity = Mathf.Max(verticalVelocity - gravity * dt, -terminalFallSpeed);
            }
            bool actionReady = supported && input.JumpPressed &&
                groundedTime >= minimumGroundTimeBeforeJump && Time.time - lastJumpTime >= jumpCooldown;
            if (actionReady && TryStartMantle()) { UpdateMantle(dt); return; }
            bool jumped = actionReady && !IsCrouching;
            if (jumped)
            {
                verticalVelocity = Mathf.Sqrt(2 * gravity * jumpHeight);
                lastJumpTime = Time.time;
                groundedTime = 0;
                supported = false;
                IsGrounded = false;
                IsWalkingSlow = false;
                OnJump?.Invoke();
            }
            Vector3 planar = supported ? Vector3.ProjectOnPlane(horizontalVelocity, GroundNormal) : horizontalVelocity;
            if (supported && planar.sqrMagnitude > 0.001f) planar = planar.normalized * horizontalVelocity.magnitude;
            float impactSpeed = Mathf.Max(incomingFallSpeed, -verticalVelocity);
            controller.stepOffset = supported ? AvailableStepOffset(planar * dt) : 0;
            Vector3 displacement = (planar + Vector3.up * verticalVelocity) * dt;
            if (supported && !jumped) displacement.y -= Mathf.Min(groundSnapDistance, Mathf.Max(0, support.distance - 0.08f));
            Vector3 beforeMove = transform.position;
            var flags = controller.Move(displacement);
            Velocity = (transform.position - beforeMove) / dt;
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
            bool groundNow = ProbeGround(out var ground);
            IsGrounded = !jumped && verticalVelocity <= 0 && groundNow &&
                ((flags & CollisionFlags.Below) != 0 || ground.distance <= 0.08f + controller.skinWidth + 0.02f);
            if (IsGrounded)
            {
                GroundNormal = ground.normal;
                verticalVelocity = -groundStickSpeed;
                if (!wasGrounded) OnLand?.Invoke(impactSpeed);
            }
            SetSprinting(sprint && IsGrounded && Speed > 0.15f);
            IsWalkingSlow &= IsGrounded;
        }

        private float AvailableStepOffset(Vector3 motion)
        {
            float maximum = Mathf.Min(defaultStepOffset, controller.height * 0.4f);
            float radius = controller.radius + standingClearance;
            Vector3 head = transform.position + Vector3.up * (controller.height - controller.radius);
            // Test the head's entire horizontal travel, including the entrance of an overhang.
            // CharacterController's automatic step lift must fit as well as the resting capsule.
            if (HeadSpaceClear(head, motion, radius, maximum)) return maximum;
            float low = 0, high = maximum;
            for (int i = 0; i < 6; i++)
            {
                float middle = (low + high) * 0.5f;
                if (HeadSpaceClear(head, motion, radius, middle)) low = middle; else high = middle;
            }
            return low;
        }

        private bool HeadSpaceClear(Vector3 head, Vector3 motion, float radius, float lift)
        {
            Vector3 start = head + Vector3.up * lift;
            int count = Physics.OverlapCapsuleNonAlloc(start, start + motion, radius, overlaps,
                collisionMask, QueryTriggerInteraction.Ignore);
            return NoExternalOverlap(count);
        }

        private bool NoExternalOverlap(int count)
        {
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++) if (!IsSelf(overlaps[i])) return false;
            return true;
        }

        private bool RaycastWorld(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearest)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, groundHits, distance, collisionMask,
                QueryTriggerInteraction.Ignore);
            nearest = default;
            if (count == groundHits.Length) return false;
            float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (IsSelf(groundHits[i].collider) || groundHits[i].distance >= best) continue;
                nearest = groundHits[i];
                best = nearest.distance;
            }
            return best < float.PositiveInfinity;
        }

        private void CapsuleAt(Vector3 feet, out Vector3 bottom, out Vector3 top, out float radius)
        {
            // Match the contact tolerance of the controller, so an accepted floor contact is not
            // mistaken for initial penetration by the upward sweep.
            radius = Mathf.Max(0.01f, controller.radius - controller.skinWidth - 0.005f);
            bottom = feet + Vector3.up * controller.radius;
            top = feet + Vector3.up * (controller.height - controller.radius);
        }

        private bool CapsuleClear(Vector3 feet)
        {
            CapsuleAt(feet, out var bottom, out var top, out float radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps, collisionMask,
                QueryTriggerInteraction.Ignore);
            return NoExternalOverlap(count);
        }

        private bool MantleSegmentClear(Vector3 from, Vector3 to)
        {
            if (!CapsuleClear(to)) return false;
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.000001f) return true;
            CapsuleAt(from, out var bottom, out var top, out float radius);
            int count = Physics.CapsuleCastNonAlloc(bottom, top, radius, delta.normalized, groundHits,
                delta.magnitude, collisionMask, QueryTriggerInteraction.Ignore);
            if (count == groundHits.Length) return false;
            for (int i = 0; i < count; i++) if (!IsSelf(groundHits[i].collider)) return false;
            return true;
        }

        private bool TryStartMantle()
        {
            Vector3 feet = transform.position;
            Vector3 forward = transform.forward;
            if (!RaycastWorld(feet + Vector3.up * minimumMantleHeight, forward,
                controller.radius + mantleDetectionDistance, out var front)) return false;
            if (Vector3.Dot(front.normal, forward) > -0.5f) return false;
            Vector3 sample = front.point + forward * 0.05f;
            sample.y = feet.y + maximumMantleHeight + standingClearance;
            if (!RaycastWorld(sample, Vector3.down, maximumMantleHeight + standingClearance,
                out var surface)) return false;
            float height = surface.point.y - feet.y;
            if (height < minimumMantleHeight || height > maximumMantleHeight ||
                surface.normal.y < Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad)) return false;

            Vector3 landingSample = front.point + forward * (controller.radius + mantleForwardDistance);
            landingSample.y = surface.point.y + standingClearance;
            if (!RaycastWorld(landingSample, Vector3.down, height + standingClearance + 0.1f,
                out var destination) || destination.normal.y < Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad)) return false;
            Vector3 target = destination.point + Vector3.up * standingClearance;
            Vector3 lift = new Vector3(feet.x, surface.point.y + standingClearance, feet.z);
            Vector3 across = new Vector3(target.x, lift.y, target.z);
            // Check full support at the destination and both swept capsule segments before committing.
            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = (i < 2 ? transform.right : forward) * controller.radius * (i % 2 == 0 ? 0.85f : -0.85f);
                if (!RaycastWorld(target + offset + Vector3.up * 0.08f, Vector3.down, 0.16f,
                    out var edge) || edge.normal.y < Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad)) return false;
            }
            if (!MantleSegmentClear(feet, lift) || !MantleSegmentClear(lift, across) ||
                !MantleSegmentClear(across, target)) return false;
            mantleStart = feet;
            mantleLift = lift;
            mantleAcross = across;
            mantleTarget = target;
            mantleElapsed = 0;
            IsMantling = true;
            IsWalkingSlow = IsGrounded = false;
            groundedTime = 0;
            horizontalVelocity = Velocity = Vector3.zero;
            verticalVelocity = 0;
            controller.stepOffset = 0;
            SetSprinting(false);
            return true;
        }

        private void UpdateMantle(float dt)
        {
            mantleElapsed += dt;
            float t = Mathf.Clamp01(mantleElapsed / mantleDuration);
            bool descend = mantleAcross.y - mantleTarget.y > 0.01f;
            float acrossEnd = descend ? 0.8f : 1;
            Vector3 next = t < 0.45f
                ? Vector3.Lerp(mantleStart, mantleLift, Mathf.SmoothStep(0, 1, t / 0.45f))
                : t < acrossEnd
                    ? Vector3.Lerp(mantleLift, mantleAcross, Mathf.SmoothStep(0, 1, (t - 0.45f) / (acrossEnd - 0.45f)))
                    : Vector3.Lerp(mantleAcross, mantleTarget, Mathf.SmoothStep(0, 1, (t - acrossEnd) / Mathf.Max(0.01f, 1 - acrossEnd)));
            // Recheck moving obstacles every frame; never teleport or turn off collision resolution.
            if (!MantleSegmentClear(transform.position, next)) { FinishMantle(); return; }
            Vector3 beforeMove = transform.position;
            controller.Move(next - beforeMove);
            Velocity = (transform.position - beforeMove) / dt;
            if (Vector3.Distance(transform.position, next) > controller.skinWidth + 0.02f || t >= 1)
                FinishMantle();
        }

        private void FinishMantle()
        {
            IsMantling = false;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0;
            groundedTime = 0;
            lastJumpTime = Time.time;
            IsGrounded = ProbeGround(out var ground) && ground.distance <= 0.08f + controller.skinWidth + 0.02f;
            GroundNormal = IsGrounded ? ground.normal : Vector3.up;
        }

        private bool ProbeGround(out RaycastHit best)
        {
            float radius = controller.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (radius + 0.08f);
            int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits,
                groundSnapDistance + 0.08f, collisionMask, QueryTriggerInteraction.Ignore);
            best = default;
            float nearest = float.PositiveInfinity;
            float minimumUp = Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (IsSelf(hit.collider) || hit.normal.y < minimumUp || hit.distance >= nearest) continue;
                nearest = hit.distance;
                best = hit;
            }
            return nearest < float.PositiveInfinity;
        }

        private void UpdateStance(float dt)
        {
            // Releasing the cursor preserves stance, so a debug pause cannot stand the player up.
            bool crouch = input.HasControl ? input.CrouchHeld : IsCrouching;
            if (!crouch && controller.height < standingHeight && !CanStand()) crouch = true;
            float target = crouch ? crouchingHeight : standingHeight;
            float height = Mathf.MoveTowards(controller.height, target, heightChangeSpeed * dt);
            // Do not recreate the native controller's contact state once the stance has settled.
            if (!Mathf.Approximately(controller.height, height))
            {
                controller.height = height;
                controller.center = Vector3.up * height * 0.5f;
            }
            bool state = crouch || controller.height < standingHeight - 0.01f;
            if (state != IsCrouching) { IsCrouching = state; OnCrouchChanged?.Invoke(state); }
            UpdateEyeHeight();
        }

        public bool CanStand()
        {
            if (controller == null) return false;
            float radius = Mathf.Max(0.01f, controller.radius - controller.skinWidth * 0.5f);
            // Only test the additional upper volume; the existing capsule already resolves the floor.
            Vector3 bottom = transform.position + Vector3.up * Mathf.Max(radius, controller.height - radius);
            Vector3 top = transform.position + Vector3.up * (standingHeight - radius + standingClearance);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps, collisionMask, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++) if (!IsSelf(overlaps[i])) return false;
            return true;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // A ceiling normal can contain a tiny horizontal component. Normalizing that component
            // used to turn a ceiling contact into a full-strength wall and erase forward momentum.
            if (hit.normal.y < -0.01f || hit.normal.y >= Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad)) return;
            Vector3 normal = new Vector3(hit.normal.x, 0, hit.normal.z).normalized;
            float into = Vector3.Dot(horizontalVelocity, normal);
            if (into < 0) horizontalVelocity -= normal * into;
        }
        private bool IsSelf(Collider other) => other == null || other.transform == transform || other.transform.IsChildOf(transform);
        private void UpdateEyeHeight()
        {
            if (viewRoot != null) viewRoot.localPosition = new Vector3(0, controller.height - eyeInset, 0);
        }
        public void SetMotorEnabled(bool value)
        {
            motorEnabled = value;
            IsMantling = IsWalkingSlow = false;
            horizontalVelocity = Velocity = Vector3.zero;
            verticalVelocity = 0;
            SetSprinting(false);
        }
        private void SetSprinting(bool value)
        {
            if (value == IsSprinting) return;
            IsSprinting = value;
            if (value) OnStartedSprinting?.Invoke(); else OnStoppedSprinting?.Invoke();
        }
        private void OnDisable() { SetSprinting(false); IsMantling = IsWalkingSlow = false; Velocity = horizontalVelocity = Vector3.zero; }
        private void OnValidate() => ValidateSettings();
        private void ValidateSettings()
        {
            var cc = GetComponent<CharacterController>();
            float minimum = cc != null ? cc.radius * 2 + 0.05f : 0.7f;
            crouchingHeight = Mathf.Max(minimum, crouchingHeight);
            standingHeight = Mathf.Max(crouchingHeight, standingHeight);
            sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
            gravity = Mathf.Max(0.1f, gravity);
            slowWalkSpeed = Mathf.Clamp(slowWalkSpeed, 0, walkSpeed);
            crouchSlowWalkSpeed = Mathf.Clamp(crouchSlowWalkSpeed, 0, crouchSpeed);
            minimumMantleHeight = Mathf.Max(0.05f, minimumMantleHeight);
            maximumMantleHeight = Mathf.Max(minimumMantleHeight, maximumMantleHeight);
            mantleDuration = Mathf.Max(0.1f, mantleDuration);
        }
    }
}
