using UnityEngine;

namespace Outbreak.Quests
{
    public enum QuestType { Collect, Kill, Rescue, ExtractItem, Discover, Interact }
    public enum QuestState { Locked, Available, Active, Completed, Claimed }

    [CreateAssetMenu(menuName = "Outbreak/Quests/Quest")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private QuestType type;
        [SerializeField] private string targetId;
        [SerializeField, Min(1)] private int requiredCount = 1;
        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public QuestType Type => type;
        public string TargetId => targetId;
        public int RequiredCount => requiredCount;
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = System.Guid.NewGuid().ToString("N");
        }
    }
}
