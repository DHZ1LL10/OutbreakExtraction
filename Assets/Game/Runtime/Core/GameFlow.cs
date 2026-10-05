using System;
using Outbreak.Economy;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Maps;
using Outbreak.Persistence;
using Outbreak.Raids;

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
            checkpoint.Loadout.Clear();
            State = GameState.LoadingRaid;
            // The on-disk profile no longer owns deployed gear, even if the application closes.
            if (!Persist(checkpoint)) { State = GameState.Hub; return false; }
            Profile = checkpoint;
            Session = session;
            State = GameState.Raid;
            return true;
        }
        public bool ExtractSuccessfully()
        {
            if (State != GameState.Raid || Session == null) return Reject("No active raid.");
            var candidate = CloneProfile();
            if (!candidate.Stash.TryAddRange(Session.Survivors())) return Reject("Stash full; raid remains active. Remove loot or equipment and retry.");
            if (!Persist(candidate)) return false;
            Profile = candidate;
            Session.Close();
            Session = null;
            State = GameState.RaidSuccess;
            return true;
        }
        public bool FailRaid()
        {
            if (State != GameState.Raid || Session == null) return Reject("No active raid.");
            Session.Close();
            Session = null;
            State = GameState.RaidFailed;
            LastMessage = "Raid failed. All deployed equipment and loot lost.";
            return true;
        }
        public bool ReturnToHub()
        {
            if (State != GameState.RaidSuccess && State != GameState.RaidFailed) return Reject("Raid must end before returning to Hub.");
            State = GameState.Hub;
            return true;
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
