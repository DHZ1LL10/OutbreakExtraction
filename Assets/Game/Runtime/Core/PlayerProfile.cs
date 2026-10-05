using System;
using System.Collections.Generic;
using Outbreak.Economy;
using Outbreak.Inventory;
using Outbreak.Loadouts;
using Outbreak.Quests;

namespace Outbreak.Core
{
    public sealed class PlayerProfile
    {
        public Wallet Wallet { get; }
        public StashInventory Stash { get; }
        public Loadout Loadout { get; }
        public int Level { get; private set; }
        public int XP { get; private set; }
        public List<QuestProgress> Quests { get; } = new List<QuestProgress>();

        public PlayerProfile(int stashCapacity = 64, int money = 0, int level = 1, int xp = 0)
        {
            if (level < 1 || xp < 0) throw new ArgumentOutOfRangeException(nameof(level));
            Wallet = new Wallet(money);
            Stash = new StashInventory(stashCapacity);
            Loadout = new Loadout();
            Level = level;
            XP = xp;
        }
        public bool AddXP(int amount)
        {
            if (amount < 0 || (long)XP + amount > int.MaxValue) return false;
            XP += amount;
            return true;
        }
        public bool SetLevel(int level)
        {
            if (level < 1) return false;
            Level = level;
            return true;
        }
    }
}
