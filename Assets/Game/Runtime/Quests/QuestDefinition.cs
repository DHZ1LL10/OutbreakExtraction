using UnityEngine;

namespace Outbreak.Quests
{
    public enum QuestType { Collect, Kill, Rescue, ExtractItem, Discover, Interact }
    public enum QuestState { Locked = 0, Available = 1, Active = 2, Completed = 3, Claimed = 4, ReadyToTurnIn = 5 }

    [CreateAssetMenu(menuName = "Outbreak/Quests/Quest")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private QuestType type;
        [SerializeField] private string targetId;
        [SerializeField, Min(1)] private int requiredCount = 1;
        [SerializeField] private bool requiresExtraction;
        [SerializeField, Min(0)] private int rewardMoney;
        [SerializeField, Min(0)] private int rewardXP;
        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public QuestType Type => type;
        public string TargetId => targetId;
        public int RequiredCount => requiredCount;
        public bool RequiresExtraction => requiresExtraction;
        public int RewardMoney => rewardMoney;
        public int RewardXP => rewardXP;
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = System.Guid.NewGuid().ToString("N");
        }
    }
}
