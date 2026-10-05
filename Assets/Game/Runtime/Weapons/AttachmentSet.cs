using System;
using System.Collections.Generic;
using UnityEngine;

namespace Outbreak.Weapons
{
    // Read-only to callers; WeaponRuntimeState owns mutations and ammo reconciliation.
    public sealed class AttachmentSet
    {
        private readonly AttachmentDefinition[] slots = new AttachmentDefinition[Enum.GetValues(typeof(AttachmentSlot)).Length];
        public AttachmentDefinition this[AttachmentSlot slot] => slots[(int)slot];
        public IEnumerable<AttachmentDefinition> Equipped
        {
            get { foreach (var entry in slots) if (entry != null) yield return entry; }
        }
        internal void Set(AttachmentSlot slot, AttachmentDefinition definition) => slots[(int)slot] = definition;
        internal void Replace(AttachmentSet other) { Array.Copy(other.slots, slots, slots.Length); }
        // Boundary for a future save / raid / workbench adapter: resolve IDs through its catalog,
        // then call TryApplyAttachments on the target runtime state. No inventory ownership here.
        public string[] ExportItemIds()
        {
            var result = new List<string>();
            foreach (var entry in Equipped) result.Add(entry.ItemId);
            return result.ToArray();
        }
    }

    public sealed class EffectiveWeaponStats
    {
        public float RecoilVertical { get; }
        public float RecoilHorizontal { get; }
        public float HipSpread { get; }
        public float AdsSpread { get; }
        public float AdsSpeed { get; }
        public float ReloadDuration { get; }
        public int MagazineCapacity { get; }
        public float NoiseMultiplier { get; }
        public float MuzzleFlashMultiplier { get; }

        public EffectiveWeaponStats(WeaponDefinition definition, AttachmentSet attachments)
        {
            RecoilVertical = Calculate(definition.recoilVertical, attachments, m => m.recoilVertical);
            RecoilHorizontal = Calculate(definition.recoilHorizontal, attachments, m => m.recoilHorizontal);
            HipSpread = Calculate(definition.hipSpread, attachments, m => m.hipSpread);
            AdsSpread = Calculate(definition.adsSpread, attachments, m => m.adsSpread);
            AdsSpeed = Mathf.Max(0.1f, Calculate(definition.adsSpeed, attachments, m => m.adsSpeed));
            ReloadDuration = Mathf.Max(0.05f, Calculate(definition.reloadDuration, attachments, m => m.reloadDuration));
            MagazineCapacity = Mathf.Clamp(Mathf.RoundToInt(Calculate(definition.magazineCapacity, attachments, m => m.magazineCapacity)), 1, 1000);
            NoiseMultiplier = Calculate(definition.noiseMultiplier, attachments, m => m.noise);
            MuzzleFlashMultiplier = Calculate(definition.muzzleFlashMultiplier, attachments, m => m.muzzleFlash);
        }
        // Order-independent stacking: (base + sum(additives)) * product(multipliers).
        private static float Calculate(float basis, AttachmentSet set, Func<AttachmentModifiers, StatModifier> select)
        {
            double additive = 0, multiplier = 1;
            foreach (var attachment in set.Equipped)
            {
                var modifier = select(attachment.modifiers);
                additive += modifier.additive; multiplier *= modifier.multiplier;
            }
            return (float)Math.Max(0, Math.Min(10000, (basis + additive) * multiplier));
        }
    }
}
