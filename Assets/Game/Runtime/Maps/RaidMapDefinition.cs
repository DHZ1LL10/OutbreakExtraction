using System;
using System.Collections.Generic;
using UnityEngine;

namespace Outbreak.Maps
{
    [Serializable]
    public sealed class ExtractionDefinition
    {
        public string id;
        public string displayName;
        [Min(0)] public float durationSeconds = 10;
        public bool requiresQuestItem;
        public string requiredItemId;
    }

    [CreateAssetMenu(menuName = "Outbreak/Maps/Raid Map")]
    public sealed class RaidMapDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField, Range(1, 5)] private int difficulty = 1;
        [SerializeField, Min(0)] private float lootMultiplier = 1;
        [SerializeField, Min(0)] private float rareLootMultiplier = 1;
        [SerializeField, Min(0)] private float enemyDensityMultiplier = 1;
        [SerializeField, Range(0, 1)] private float specialInfectedChance;
        [SerializeField] private List<ExtractionDefinition> extractions = new List<ExtractionDefinition>();
        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int Difficulty => difficulty;
        public float LootMultiplier => lootMultiplier;
        public float RareLootMultiplier => rareLootMultiplier;
        public float EnemyDensityMultiplier => enemyDensityMultiplier;
        public float SpecialInfectedChance => specialInfectedChance;
        public IReadOnlyList<ExtractionDefinition> Extractions => extractions.AsReadOnly();

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(id) || difficulty < 1 || difficulty > 5 ||
                !ValidMultiplier(lootMultiplier) || !ValidMultiplier(rareLootMultiplier) ||
                !ValidMultiplier(enemyDensityMultiplier) || !ValidMultiplier(specialInfectedChance) || specialInfectedChance > 1)
                throw new ArgumentException("Invalid raid map.");
            var ids = new HashSet<string>();
            foreach (var extraction in extractions)
                if (extraction == null || string.IsNullOrWhiteSpace(extraction.id) || !ids.Add(extraction.id) ||
                    !ValidMultiplier(extraction.durationSeconds) || (extraction.requiresQuestItem && string.IsNullOrWhiteSpace(extraction.requiredItemId)))
                    throw new ArgumentException("Invalid extraction definition.");
        }

        private static bool ValidMultiplier(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = Guid.NewGuid().ToString("N");
        }
    }
}
