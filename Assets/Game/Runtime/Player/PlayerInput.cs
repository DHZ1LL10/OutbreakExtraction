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
        public bool HasControl => ControlEnabled && Application.isFocused && Cursor.lockState == CursorLockMode.Locked;

        private void Start() { if (lockCursorOnStart) CaptureCursor(); }
        private void Update()
        {
            ClearInput();
            if (Input.GetKeyDown(KeyCode.Escape)) { ReleaseCursor(); return; }
            if (!HasControl)
            {
                if (ControlEnabled && Application.isFocused && Input.GetMouseButtonDown(0)) CaptureCursor();
                return;
            }
            MoveInput = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1);
            LookInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            FireHeld = Input.GetMouseButton(0);
            FirePressed = Input.GetMouseButtonDown(0);
            AimHeld = Input.GetMouseButton(1);
            ReloadPressed = Input.GetKeyDown(KeyCode.R);
            PrimaryPressed = Input.GetKeyDown(KeyCode.Alpha1);
            SecondaryPressed = Input.GetKeyDown(KeyCode.Alpha2);
            FireModePressed = Input.GetKeyDown(KeyCode.B);
            JumpPressed = Input.GetKeyDown(jumpKey);
            SprintHeld = Input.GetKey(sprintKey);
            CrouchHeld = Input.GetKey(crouchKey);
            SlowWalkHeld = Input.GetKey(slowWalkKey);
            LeanInput = (Input.GetKey(leanRightKey) ? 1 : 0) - (Input.GetKey(leanLeftKey) ? 1 : 0);
        }
        public void SetControlEnabled(bool value)
        {
            ControlEnabled = value;
            ClearInput();
            if (!value) ReleaseCursor();
        }
        public void CaptureCursor()
        {
            if (!ControlEnabled) return;
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
