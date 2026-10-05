using UnityEngine;

namespace Outbreak.Player
{
    public interface IPlayerLeanState
    {
        float LeanAmount { get; }
        bool IsLeaning { get; }
    }

    [RequireComponent(typeof(PlayerMotor)), DisallowMultipleComponent]
    public sealed class PlayerDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool showOverlay = true;
        private PlayerMotor motor;
        private IPlayerLeanState lean;
        private void Awake() => motor = GetComponent<PlayerMotor>();
        private void Start() => lean = GetComponentInChildren<IPlayerLeanState>();
        private void OnGUI()
        {
            if (!showOverlay || motor == null) return;
            GUI.Box(new Rect(12, 12, 340, 228), "OUTBREAK - FPS Debug");
            GUI.Label(new Rect(24, 38, 320, 200),
                $"Speed: {motor.Speed:F2} m/s\nGrounded: {motor.IsGrounded}\nSprinting: {motor.IsSprinting}\nCrouching: {motor.IsCrouching}\nSlowWalk: {motor.IsWalkingSlow}\nLean: {lean?.LeanAmount ?? 0:F2}\nMantling: {motor.IsMantling}\nVertical Velocity: {motor.VerticalVelocity:F2} m/s\nWASD / Space / Shift / Ctrl / Alt / Q-E\nEscape: liberar cursor | Click: capturar");
        }
    }
}
