using UnityEngine;

namespace Outbreak.Weapons
{
    [RequireComponent(typeof(Collider))]
    public sealed class DamageHitbox : MonoBehaviour
    {
        public HitZone zone;
        public IDamageable Receiver => GetComponentInParent<IDamageable>();
    }
}
