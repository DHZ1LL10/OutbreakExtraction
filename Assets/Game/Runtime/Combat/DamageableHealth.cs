using System;
using Outbreak.Weapons;
using UnityEngine;

namespace Outbreak.Combat
{
    public abstract class DamageableHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private float maxHealth = 100;
        private HealthState state;
        private HealthState State => state ?? (state = new HealthState(maxHealth));
        public float MaxHealth => State.MaxHealth;
        public float CurrentHealth => State.CurrentHealth;
        public bool IsAlive => State.IsAlive;
        public event Action<DamageInfo> OnDamaged { add => State.OnDamaged += value; remove => State.OnDamaged -= value; }
        public event Action<float> OnHealed { add => State.OnHealed += value; remove => State.OnHealed -= value; }
        public event Action<DamageInfo> OnDied { add => State.OnDied += value; remove => State.OnDied -= value; }
        public void ApplyDamage(DamageInfo damage) => State.ApplyDamage(damage);
        public void Heal(float amount) => State.Heal(amount);
    }
}
