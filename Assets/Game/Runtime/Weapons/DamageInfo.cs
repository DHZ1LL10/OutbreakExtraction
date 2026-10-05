using UnityEngine;

namespace Outbreak.Weapons
{
    public interface IDamageable { void ApplyDamage(DamageInfo damage); }
    public enum HitZone { Body, Head, Limb }
    public readonly struct DamageInfo
    {
        public float Damage { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public GameObject Source { get; }
        public bool IsHeadshot { get; }
        public string WeaponId { get; }
        public DamageInfo(WeaponDefinition weapon, HitZone zone, Vector3 point, Vector3 normal, GameObject source)
        {
            IsHeadshot = zone == HitZone.Head;
            Damage = weapon.damage * (IsHeadshot ? weapon.headshotMultiplier : 1);
            HitPoint = point; HitNormal = normal; Source = source; WeaponId = weapon.WeaponId;
        }
    }
    // Implemented by Bodycam, avoiding a Game -> Bodycam assembly cycle.
    public interface IWeaponCameraFeedback
    {
        void SetWeaponAim(float weight, float fov);
        void AddWeaponKick(float degrees);
    }
}
