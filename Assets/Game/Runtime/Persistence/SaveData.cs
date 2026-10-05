using System;
using System.Collections.Generic;
using Outbreak.Loadouts;
using Outbreak.Quests;

namespace Outbreak.Persistence
{
    [Serializable]
    public sealed class StackData
    {
        public string itemId;
        public int quantity;
    }
    [Serializable]
    public sealed class EquipmentData
    {
        public LoadoutSlot slot;
        public StackData stack;
    }
    [Serializable]
    public sealed class SaveData
    {
        public string format;
        public int version;
        public int money;
        public int level;
        public int xp;
        public int stashCapacity;
        public List<StackData> stash;
        public List<EquipmentData> equipment;
        // Derived ownership summaries, never a second source of physical items.
        public List<StackData> ownedWeapons;
        public List<StackData> ownedAttachments;
        public List<QuestProgress> quests;
    }
}
