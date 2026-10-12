using UnityEngine;

namespace Outbreak
{
    // Installed only on the two presentation scenes. Standalone diagnostic scenes retain their HUD.
    [DisallowMultipleComponent]
    public sealed class PresentationMode : MonoBehaviour
    {
        public static bool Active { get; private set; }
        public static bool DebugVisible { get; private set; }
        public static bool ShowDiagnostics => !Active || DebugVisible;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() { Active = DebugVisible = false; }
        private void OnEnable() { Active = true; DebugVisible = false; }
        private void Update() { if (Input.GetKeyDown(KeyCode.F10)) ToggleDiagnostics(); }
        public void ToggleDiagnostics() => DebugVisible = !DebugVisible;
        private void OnDisable() { Active = DebugVisible = false; }
    }
}
