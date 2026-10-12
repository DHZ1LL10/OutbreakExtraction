using UnityEngine;

namespace Outbreak.Player
{
    [DefaultExecutionOrder(-300), DisallowMultipleComponent]
    public sealed class PlayerInput : MonoBehaviour
    {
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode slowWalkKey = KeyCode.LeftAlt;
        [SerializeField] private KeyCode leanLeftKey = KeyCode.Q;
        [SerializeField] private KeyCode leanRightKey = KeyCode.E;
        [SerializeField] private bool lockCursorOnStart = true;
        [SerializeField] private bool blockMovementWhileSuppressed;
        public bool FireHeld { get; private set; }
        public bool FirePressed { get; private set; }
        public bool AimHeld { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool PrimaryPressed { get; private set; }
        public bool SecondaryPressed { get; private set; }
        public bool FireModePressed { get; private set; }
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool SlowWalkHeld { get; private set; }
        public float LeanInput { get; private set; }
        public bool ControlEnabled { get; private set; } = true;
        public bool InteractionFocused { get; private set; }
        private readonly System.Collections.Generic.HashSet<object> suppressionOwners = new System.Collections.Generic.HashSet<object>();
        private bool waitForMouseRelease;
        private int resumeFrame = -1;
        public bool GameplaySuppressed => suppressionOwners.Count > 0;
        public bool HasControl => ControlEnabled && !GameplaySuppressed && Application.isFocused && Cursor.lockState == CursorLockMode.Locked;

        private void Start() { if (lockCursorOnStart) CaptureCursor(); }
        private void Update()
        {
            ClearInput();
            if (GameplaySuppressed)
            {
                // Walk away from a container without giving the UI mouse back to the FPS.
                if (!blockMovementWhileSuppressed && ControlEnabled && Application.isFocused)
                    MoveInput = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1);
                return;
            }
            if (Time.frameCount <= resumeFrame) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { ReleaseCursor(); return; }
            if (!HasControl)
            {
                if (ControlEnabled && Application.isFocused && Input.GetMouseButtonDown(0)) CaptureCursor();
                return;
            }
            MoveInput = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1);
            LookInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            if (waitForMouseRelease && !Input.GetMouseButton(0) && !Input.GetMouseButton(1)) waitForMouseRelease = false;
            FireHeld = !waitForMouseRelease && Input.GetMouseButton(0);
            FirePressed = !waitForMouseRelease && Input.GetMouseButtonDown(0);
            AimHeld = !waitForMouseRelease && Input.GetMouseButton(1);
            ReloadPressed = Input.GetKeyDown(KeyCode.R);
            PrimaryPressed = Input.GetKeyDown(KeyCode.Alpha1);
            SecondaryPressed = Input.GetKeyDown(KeyCode.Alpha2);
            FireModePressed = Input.GetKeyDown(KeyCode.B);
            JumpPressed = Input.GetKeyDown(jumpKey);
            SprintHeld = Input.GetKey(sprintKey);
            CrouchHeld = Input.GetKey(crouchKey);
            SlowWalkHeld = Input.GetKey(slowWalkKey);
            LeanInput = ResolveLean(Input.GetKey(leanLeftKey), Input.GetKey(leanRightKey), InteractionFocused, GameplaySuppressed);
        }
        public static float ResolveLean(bool left, bool right, bool interactionFocused, bool suppressed) =>
            suppressed ? 0 : (right && !interactionFocused ? 1 : 0) - (left ? 1 : 0);
        public void SetGameplaySuppressed(object owner, bool value)
        {
            if (owner == null) throw new System.ArgumentNullException(nameof(owner));
            if (value) suppressionOwners.Add(owner); else suppressionOwners.Remove(owner);
            ClearInput();
            if (GameplaySuppressed) ReleaseCursor();
            else { resumeFrame = Time.frameCount + 1; CaptureCursor(); }
        }
        public void SetInteractionFocused(bool value) => InteractionFocused = value;
        public void SetControlEnabled(bool value)
        {
            ControlEnabled = value;
            ClearInput();
            if (!value) ReleaseCursor();
        }
        public void CaptureCursor()
        {
            if (!isActiveAndEnabled || !ControlEnabled || GameplaySuppressed || !Application.isFocused) return;
            ClearInput();
            waitForMouseRelease = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        public void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ClearInput();
        }
        private void OnApplicationFocus(bool focused) { if (!focused) ReleaseCursor(); }
        private void OnDisable() => ReleaseCursor();
        private void ClearInput()
        {
            MoveInput = LookInput = Vector2.zero;
            JumpPressed = SprintHeld = CrouchHeld = SlowWalkHeld = false;
            LeanInput = 0;
            FireHeld = FirePressed = AimHeld = ReloadPressed = PrimaryPressed = SecondaryPressed = FireModePressed = false;
        }
    }
}
