using System;
using System.Collections.Generic;
using System.Linq;
using Outbreak.Core;
using Outbreak.Loadouts;
using Outbreak.Weapons;

namespace Outbreak.Hub
{
    public static class HubLoadoutAdapter
    {
        public static bool ApplyPreset(WeaponRuntimeState state, PlayerProfile profile, AttachmentDefinition[] catalog)
        {
            if (state == null) return true;
            if (!profile.WeaponAttachments.TryGetValue(state.Definition.WeaponId, out var ids)) return true;
            var resolved = ids.Select(id => catalog.FirstOrDefault(a => a != null && a.ItemId == id)).ToArray();
            return state.TryApplyAttachments(resolved);
        }
        public static bool Configure(GameFlow flow, WeaponDefinition weapon, AttachmentSlot slot, AttachmentDefinition attachment, AttachmentDefinition[] catalog)
        {
            if (flow.State != GameState.Hub || weapon == null || !Enum.IsDefined(typeof(AttachmentSlot), slot) ||
                (attachment != null && (attachment.slot != slot || !attachment.IsCompatible(weapon)))) return false;
            var state = new WeaponRuntimeState(weapon, 0, 0);
            if (!ApplyPreset(state, flow.Profile, catalog)) return false;
            if (attachment == null)
            { if (state.Attachments[slot] != null && !state.TryRemoveAttachment(slot)) return false; }
            else if (!state.TryEquipAttachment(attachment)) return false;
            return flow.CommitHubChange(f => { f.Profile.WeaponAttachments[weapon.WeaponId] = state.Attachments.ExportItemIds(); return true; });
        }
        public static bool CanDeploy(GameFlow flow, WeaponDefinition[] weapons, AttachmentDefinition[] attachments)
        {
            if (flow.State != GameState.Hub || flow.Session != null) return false;
            bool equipped = false;
            foreach (var slot in new[] { LoadoutSlot.Primary, LoadoutSlot.Secondary })
            {
                var stack = flow.Profile.Loadout.Get(slot); if (stack == null) continue;
                var weapon = weapons.FirstOrDefault(w => w != null && w.WeaponId == stack.Item.Id);
                // Existing test viewmodels are tied to rifle-primary/pistol-secondary.
                if (weapon == null || weapon.weaponClass != (slot == LoadoutSlot.Primary ? WeaponClass.Rifle : WeaponClass.Pistol)) return false;
                if (!ApplyPreset(new WeaponRuntimeState(weapon, 0, 0), flow.Profile, attachments)) return false;
                equipped = true;
            }
            return equipped;
        }
    }
}
