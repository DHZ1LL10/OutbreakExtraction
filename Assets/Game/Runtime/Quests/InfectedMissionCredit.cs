using Outbreak.Combat;
using Outbreak.Infected;
using Outbreak.Raids;
using Outbreak.Weapons;
using UnityEngine;

namespace Outbreak.Quests
{
    [RequireComponent(typeof(InfectedHealth))]
    public sealed class InfectedMissionCredit : MonoBehaviour
    {
        [SerializeField] private bool runner;
        private InfectedHealth health;
        private void Awake() => health = GetComponent<InfectedHealth>();
        private void OnEnable() { if (health != null) health.OnDied += Credit; }
        private void OnDisable() { if (health != null) health.OnDied -= Credit; }
        private void Credit(DamageInfo damage)
        {
            if (damage.Source == null || damage.Source.GetComponentInParent<FirearmController>() == null) return;
            FindFirstObjectByType<RaidRuntimeContext>()?.Flow?.RecordInfectedKill(runner);
        }
    }
}
