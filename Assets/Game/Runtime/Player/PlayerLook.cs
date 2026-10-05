using UnityEngine;

namespace Outbreak.Player
{
    [DefaultExecutionOrder(-200), RequireComponent(typeof(PlayerInput)), DisallowMultipleComponent]
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private Transform viewRoot;
        [SerializeField, Min(0)] private float sensitivity = 1.7f;
        [SerializeField] private bool invertY;
        [SerializeField, Range(-89, 0)] private float minimumPitch = -80;
        [SerializeField, Range(0, 89)] private float maximumPitch = 80;
        private PlayerInput input;
        private float pitch;
        private Vector2 recoil;
        private float recoilRecovery = 5;
        public void AddAimRecoil(float vertical, float horizontal, float recovery)
        {
            recoil += new Vector2(-vertical, horizontal);
            recoil.x = Mathf.Clamp(recoil.x, -20, 20);
            recoil.y = Mathf.Clamp(recoil.y, -8, 8);
            recoilRecovery = Mathf.Max(0.1f, recovery);
            ApplyView();
        }
        private void ApplyView()
        {
            if (viewRoot != null) viewRoot.localRotation = Quaternion.Euler(
                Mathf.Clamp(pitch + recoil.x, minimumPitch, maximumPitch), recoil.y, 0);
        }
        public float Pitch => pitch;
        public Vector2 LookDelta { get; private set; }

        private void Awake()
        {
            input = GetComponent<PlayerInput>();
            if (viewRoot == null)
            {
                Debug.LogError("[OUTBREAK FPS] PlayerLook needs ViewRoot.", this);
                enabled = false;
            }
        }
        private void Update()
        {
            LookDelta = Vector2.zero;
            if (!input.HasControl) return;
            // Mouse axes already represent frame displacement; do not multiply by deltaTime.
            Vector2 mouse = input.LookInput * sensitivity;
            float previousPitch = pitch;
            transform.Rotate(0, mouse.x, 0, Space.Self);
            pitch = Mathf.Clamp(pitch + mouse.y * (invertY ? 1 : -1), minimumPitch, maximumPitch);
            LookDelta = new Vector2(mouse.x, pitch - previousPitch);
            // Mouse remains independent so the player can counter the recovering recoil offset.
            recoil = Vector2.Lerp(recoil, Vector2.zero, 1 - Mathf.Exp(-recoilRecovery * Time.deltaTime));
            ApplyView();
        }
    }
}
