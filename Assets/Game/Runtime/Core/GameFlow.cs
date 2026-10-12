using System;
using System.Collections.Generic;
using System.Linq;
using Outbreak.Economy;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Maps;
using Outbreak.Persistence;
using Outbreak.Raids;
using Outbreak.Quests;

namespace Outbreak.Core
{
    public enum GameState { Hub, LoadingRaid, Raid, RaidSuccess, RaidFailed }

    public sealed class GameFlow
    {
        private readonly ItemCatalog catalog;
        private readonly SaveService saves;
        private readonly EconomyService economy = new EconomyService();
        public PlayerProfile Profile { get; private set; }
        public RaidSession Session { get; private set; }
        public GameState State { get; private set; } = GameState.Hub;
        public string LastMessage { get; private set; }
        public QuestDefinition Mission { get; private set; }
        public QuestProgress MissionProgress => Mission == null ? null : Profile.Quests.Find(q => q.questId == Mission.Id);
        public event Action MissionChanged;
        private readonly List<QuestDefinition> assignments = new List<QuestDefinition>();
        public IReadOnlyList<QuestDefinition> Assignments => assignments;
        public bool InitializeAssignments(IEnumerable<QuestDefinition> definitions)
        {
            if (State != GameState.Hub) return false;
            var list = definitions.Where(d => d != null).Distinct().ToArray();
            if (list.Length == 0 || list.Any(d => !ValidMission(d)) || list.Select(d=>d.Id).Distinct().Count()!=list.Length) return false;
            if (!CommitHubChange(f => {
                foreach (var definition in list)
                    if (!f.Profile.Quests.Any(q=>q.questId==definition.Id)) f.Profile.Quests.Add(new QuestProgress { questId=definition.Id,state=QuestState.Available });
                return true;
            })) return false;
            assignments.Clear(); assignments.AddRange(list);
            var active = Profile.Quests.FirstOrDefault(q=>q.state==QuestState.Active || q.state==QuestState.ReadyToTurnIn);
            Mission = list.FirstOrDefault(d=>d.Id==active?.questId) ?? list[0];
            MissionChanged?.Invoke(); return true;
        }
        private bool ValidMission(QuestDefinition d) => d != null && !string.IsNullOrWhiteSpace(d.Id) && d.RequiresExtraction &&
            d.RequiredCount > 0 && d.RewardMoney >= 0 && d.RewardXP >= 0 &&
            ((d.Type == QuestType.Collect && catalog.Items.Any(i=>i.Id==d.TargetId)) ||
             (d.Type == QuestType.Kill && (d.TargetId == "infected" || d.TargetId == "runner" || d.TargetId == "normal")));
        public void RecordInfectedKill(bool runner)
        {
            if (State != GameState.Raid || Mission?.Type != QuestType.Kill || MissionProgress?.state != QuestState.Active) return;
            if (Mission.TargetId != "infected" && Mission.TargetId != (runner ? "runner" : "normal")) return;
            MissionProgress.Advance(Mission, 1); MissionChanged?.Invoke();
        }

        public bool EnableMission(QuestDefinition definition)
        {
            if (State != GameState.Hub || !ValidMission(definition) || Profile.Quests.Any(q=>q.questId!=definition.Id &&
                (q.state==QuestState.Active || q.state==QuestState.ReadyToTurnIn))) return false;
            Mission = definition;
            if (MissionProgress != null) return true;
            bool saved = CommitHubChange(f => { f.Profile.Quests.Add(new QuestProgress { questId = definition.Id, state = QuestState.Available }); return true; });
            if (saved) MissionChanged?.Invoke(); return saved;
        }
        public bool AcceptMission()
        {
            if (MissionProgress?.state != QuestState.Available || Profile.Quests.Any(q=>q.questId!=Mission.Id &&
                (q.state==QuestState.Active || q.state==QuestState.ReadyToTurnIn))) return false;
            bool saved = CommitHubChange(f => f.MissionProgress.Activate());
            if (saved) MissionChanged?.Invoke(); return saved;
        }
        public bool TurnInMission()
        {
            if (MissionProgress?.state != QuestState.ReadyToTurnIn) return false;
            bool saved = CommitHubChange(f => f.Profile.Wallet.Credit(Mission.RewardMoney) &&
                f.Profile.AddXP(Mission.RewardXP) && f.MissionProgress.TurnIn());
            if (saved) MissionChanged?.Invoke(); return saved;
        }
        private void SyncMissionLoot()
        {
            var progress = MissionProgress;
            if (progress == null || progress.state != QuestState.Active || Session == null || Mission.Type != QuestType.Collect) return;
            int count = 0;
            foreach (var stack in Session.FoundLoot)
                if (stack.Item.Id == Mission.TargetId) count = (int)Math.Min((long)count + stack.Quantity, Mission.RequiredCount);
            if (progress.count != count) { progress.count = count; MissionChanged?.Invoke(); }
        }

        public GameFlow(ItemCatalog catalog, SaveService saves)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            catalog.Validate();
            this.catalog = catalog;
            this.saves = saves ?? throw new ArgumentNullException(nameof(saves));
            Profile = new PlayerProfile();
        }

        public bool NewGame()
        {
            if (State != GameState.Hub) return Reject("Return to Hub before creating a profile.");
            Profile = new PlayerProfile();
            LastMessage = "New profile created in memory; use Save to persist.";
            return true;
        }
        public bool Save()
        {
            if (State != GameState.Hub) return Reject("Saving manually is only allowed in Hub.");
            return Persist(Profile);
        }
        // Hub stations commit existing domain operations and their save atomically.
        public bool CommitHubChange(Func<GameFlow, bool> operation)
        {
            if (State != GameState.Hub || operation == null) return Reject("Hub operation unavailable.");
            var before = CloneProfile();
            string previousMessage = LastMessage;
            try
            {
                if (!operation(this))
                {
                    Profile = before;
                    if (LastMessage == previousMessage) LastMessage = "Hub operation rejected.";
                    return false;
                }
                if (Persist(Profile)) return true;
                Profile = before; return false;
            }
            catch { Profile = before; throw; }
        }
        public bool Load()
        {
            if (State != GameState.Hub) return Reject("Loading is only allowed in Hub.");
            var result = saves.Load(catalog, out var profile, out var message);
            Profile = profile ?? new PlayerProfile();
            LastMessage = message ?? "Profile loaded.";
            return result == LoadResult.Loaded;
        }

        // Preparing a loadout moves physical items out of the stash, preventing duplicates.
        public bool Equip(LoadoutSlot slot, string id, int quantity = 1)
        {
            if (State != GameState.Hub) return Reject("Loadout can only be prepared in Hub.");
            var item = catalog.Get(id);
            if (!Loadout.Accepts(slot, item) || quantity < 1 || quantity > item.MaxStack ||
                ((int)slot < (int)LoadoutSlot.Equipment1 && quantity != 1)) return Reject("Invalid equipment.");
            var candidate = CloneProfile();
            var previous = candidate.Loadout.Get(slot);
            if (!candidate.Stash.RemoveItem(id, quantity)) return Reject("Item unavailable in stash.");
            if (previous != null && !candidate.Stash.AddItem(previous.Item, previous.Quantity)) return Reject("No room for previous equipment.");
            candidate.Loadout.Set(slot, new ItemStack(item, quantity));
            Profile = candidate;
            return true;
        }
        public bool Unequip(LoadoutSlot slot)
        {
            if (State != GameState.Hub) return Reject("Unequip is only allowed in Hub.");
            var stack = Profile.Loadout.Get(slot);
            if (stack == null || !Profile.Stash.AddItem(stack.Item, stack.Quantity)) return Reject("Empty slot or full stash.");
            Profile.Loadout.Set(slot, null);
            return true;
        }
        public bool Buy(string id, int quantity) => State == GameState.Hub && economy.Buy(Profile.Wallet, Profile.Stash, catalog.Get(id), quantity);
        public bool Sell(string id, int quantity) => State == GameState.Hub && economy.Sell(Profile.Wallet, Profile.Stash, catalog.Get(id), quantity);

        public bool StartRaid(RaidMapDefinition map, int backpackCapacity = 16)
        {
            if (State != GameState.Hub || map == null) return Reject("StartRaid requires Hub and a map.");
            map.Validate();
            var session = new RaidSession(Profile.Loadout, map, backpackCapacity);
            var checkpoint = CloneProfile();
            var quest = Mission == null ? null : checkpoint.Quests.Find(q => q.questId == Mission.Id);
            if (quest?.state == QuestState.Active) quest.count = 0;
            checkpoint.Loadout.Clear();
            State = GameState.LoadingRaid;
            // The on-disk profile no longer owns deployed gear, even if the application closes.
            if (!Persist(checkpoint)) { State = GameState.Hub; return false; }
            Profile = checkpoint;
            Session = session;
            Session.LootChanged += SyncMissionLoot;
            State = GameState.Raid;
            MissionChanged?.Invoke();
            return true;
        }
        public bool ExtractSuccessfully()
        {
            if (State != GameState.Raid || Session == null) return Reject("No active raid.");
            var candidate = CloneProfile();
            if (!candidate.Stash.TryAddRange(Session.Survivors())) return Reject("Stash full; raid remains active. Remove loot or equipment and retry.");
            var quest = Mission == null ? null : candidate.Quests.Find(q => q.questId == Mission.Id);
            quest?.ConfirmExtraction(Mission);
            if (!Persist(candidate)) return false;
            Profile = candidate;
            Session.LootChanged -= SyncMissionLoot;
            Session.Close();
            Session = null;
            State = GameState.RaidSuccess;
            MissionChanged?.Invoke();
            return true;
        }
        public bool FailRaid()
        {
            if (State != GameState.Raid || Session == null) return Reject("No active raid.");
            if (MissionProgress?.state == QuestState.Active) MissionProgress.count = 0;
            Session.LootChanged -= SyncMissionLoot;
            Session.Close();
            Session = null;
            State = GameState.RaidFailed;
            LastMessage = "Raid failed. All deployed equipment and loot lost.";
            MissionChanged?.Invoke();
            return true;
        }
        public bool ReturnToHub()
        {
            if (State != GameState.RaidSuccess && State != GameState.RaidFailed) return Reject("Raid must end before returning to Hub.");
            State = GameState.Hub;
            return true;
        }
        public bool ReturnToHubAndSave()
        {
            var previous = State;
            if (!ReturnToHub()) return false;
            if (Save()) return true;
            State = previous; return false;
        }
        private PlayerProfile CloneProfile() => SaveMapper.Restore(SaveMapper.Capture(Profile), catalog);
        private bool Persist(PlayerProfile profile)
        {
            bool success = saves.TrySave(profile, out var error);
            LastMessage = success ? "Profile saved." : error;
            return success;
        }
        private bool Reject(string message) { LastMessage = message; return false; }
    }
}
