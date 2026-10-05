using System;
using Outbreak.Weapons;

namespace Outbreak.Combat
{
    // No clock or scene dependencies. DamageInfo already contains final damage, including headshots.
    public sealed class HealthState
    {
        public float MaxHealth { get; }
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public event Action<DamageInfo> OnDamaged;
        public event Action<float> OnHealed;
        public event Action<DamageInfo> OnDied;
        public HealthState(float maximum)
        {
            if (float.IsNaN(maximum) || float.IsInfinity(maximum) || maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
            MaxHealth = CurrentHealth = maximum;
        }
        public void ApplyDamage(DamageInfo damage)
        {
            if (!IsAlive || float.IsNaN(damage.Damage) || float.IsInfinity(damage.Damage) || damage.Damage <= 0) return;
            CurrentHealth = Math.Max(0, CurrentHealth - damage.Damage);
            bool died = !IsAlive;
            OnDamaged?.Invoke(damage);
            if (died) OnDied?.Invoke(damage);
        }
        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            float restored = Math.Min(MaxHealth - CurrentHealth, amount);
            if (restored <= 0) return;
            CurrentHealth += restored; OnHealed?.Invoke(restored);
        }
    }
}
