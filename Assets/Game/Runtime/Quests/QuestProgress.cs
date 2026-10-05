using System;

namespace Outbreak.Quests
{
    [Serializable]
    public sealed class QuestProgress
    {
        public string questId;
        public QuestState state;
        public int count;

        public bool Unlock()
        {
            if (state != QuestState.Locked) return false;
            state = QuestState.Available;
            return true;
        }
        public bool Activate()
        {
            if (state != QuestState.Available) return false;
            state = QuestState.Active;
            return true;
        }
        public bool Advance(QuestDefinition definition, int amount)
        {
            if (definition == null || definition.Id != questId || state != QuestState.Active || amount <= 0 || definition.RequiredCount <= 0) return false;
            count = (int)Math.Min((long)count + amount, definition.RequiredCount);
            if (count == definition.RequiredCount) state = QuestState.Completed;
            return true;
        }
        public bool Claim()
        {
            if (state != QuestState.Completed) return false;
            state = QuestState.Claimed;
            return true;
        }
        public QuestProgress Copy() => new QuestProgress { questId = questId, state = state, count = count };
    }
}
