using UnityEngine;

namespace Outbreak.Weapons
{
    public readonly struct WeaponShotFeedback
    {
        public WeaponRuntimeState State { get; }
        public Transform Muzzle { get; }
        public Vector3 Position { get; }
        public Vector3 EjectionPosition { get; }
        public Quaternion EjectionRotation { get; }
        public float NoiseMultiplier { get; }
        public float FlashMultiplier { get; }
        public float Aim { get; }
        public WeaponShotFeedback(WeaponRuntimeState state, WeaponViewModel view, Transform fallback, float aim)
        {
            State = state; Muzzle = view != null && view.Muzzle != null ? view.Muzzle : fallback;
            Position = Muzzle.position;
            var ejection = view != null ? view.EjectionPoint : fallback;
            EjectionPosition = ejection.position; EjectionRotation = ejection.rotation;
            NoiseMultiplier = state.Stats.NoiseMultiplier; FlashMultiplier = state.Stats.MuzzleFlashMultiplier; Aim = aim;
        }
    }
    public readonly struct WeaponImpactFeedback
    {
        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public bool HitDamageable { get; }
        public WeaponImpactFeedback(Vector3 position, Vector3 normal, bool hitDamageable)
        { Position = position; Normal = normal; HitDamageable = hitDamageable; }
    }
}
