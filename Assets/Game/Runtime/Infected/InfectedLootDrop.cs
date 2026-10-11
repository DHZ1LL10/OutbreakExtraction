using Outbreak.Combat;
using Outbreak.Loot;
using Outbreak.Weapons;
using UnityEngine;

namespace Outbreak.Infected
{
    [RequireComponent(typeof(InfectedHealth)), DisallowMultipleComponent]
    public sealed class InfectedLootDrop : MonoBehaviour
    {
        [SerializeField] private LootTable lootTable;
        [SerializeField, Range(0, 1)] private float dropChance = 0.3f;
        private InfectedHealth health;
        private bool dropped;
        public void Configure(LootTable table, float chance) { lootTable = table; dropChance = Mathf.Clamp01(chance); }
        private void Awake() { health = GetComponent<InfectedHealth>(); }
        private void OnEnable() { health.OnDied += Drop; }
        private void OnDisable() { health.OnDied -= Drop; }
        private void Drop(DamageInfo damage)
        {
            if (dropped) return;
            dropped = true;
            if (lootTable == null || Random.value >= dropChance) return;
            var renderer = GetComponentInChildren<Renderer>();
            foreach (var stack in lootTable.Generate(new System.Random(Random.Range(0, int.MaxValue)), 1, null))
            {
                var pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pickup.name = "Infected loot: " + stack.Item.DisplayName;
                pickup.transform.position = transform.position + Vector3.up * 0.15f;
                pickup.transform.localScale = Vector3.one * 0.25f;
                if (renderer != null) pickup.GetComponent<Renderer>().sharedMaterial = renderer.sharedMaterial;
                pickup.AddComponent<WorldItem>().Initialize(stack);
            }
        }
    }
}
