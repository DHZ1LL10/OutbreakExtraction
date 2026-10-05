using System;
using Outbreak.Items;
using UnityEngine;

namespace Outbreak.Weapons
{
    // Values are stable for future save adapters. Add slots here when their systems exist.
    public enum AttachmentSlot { Optic, Muzzle, Magazine, Grip }
    [Flags] public enum AttachmentSlots { None = 0, Optic = 1, Muzzle = 2, Magazine = 4, Grip = 8, All = 15 }
    [Flags] public enum AttachmentWeaponClasses { Pistol = 1, Rifle = 2, All = 3 }

    [Serializable]
    public struct StatModifier
    {
        public float additive;
        [Min(0)] public float multiplier;
        public StatModifier(float multiplier, float additive = 0) { this.multiplier = multiplier; this.additive = additive; }
        public static StatModifier Identity => new StatModifier(1);
        public bool IsValid => !float.IsNaN(additive) && !float.IsInfinity(additive) &&
            !float.IsNaN(multiplier) && !float.IsInfinity(multiplier) && multiplier >= 0;
    }

    [Serializable]
    public sealed class AttachmentModifiers
    {
        public StatModifier recoilVertical = StatModifier.Identity;
        public StatModifier recoilHorizontal = StatModifier.Identity;
        public StatModifier hipSpread = StatModifier.Identity;
        public StatModifier adsSpread = StatModifier.Identity;
        public StatModifier adsSpeed = StatModifier.Identity;
        public StatModifier reloadDuration = StatModifier.Identity;
        public StatModifier magazineCapacity = StatModifier.Identity;
        public StatModifier noise = StatModifier.Identity;
        public StatModifier muzzleFlash = StatModifier.Identity;
        public bool IsValid => recoilVertical.IsValid && recoilHorizontal.IsValid && hipSpread.IsValid &&
            adsSpread.IsValid && adsSpeed.IsValid && reloadDuration.IsValid && magazineCapacity.IsValid && noise.IsValid && muzzleFlash.IsValid;
    }

    [CreateAssetMenu(menuName = "Outbreak/Weapons/Attachment")]
    public sealed class AttachmentDefinition : ScriptableObject
    {
        public ItemDefinition item;
        public AttachmentSlot slot;
        public AttachmentWeaponClasses compatibleClasses = AttachmentWeaponClasses.All;
        [Tooltip("Empty allows any weapon in the supported classes. Uses existing ItemDefinition IDs.")]
        public string[] compatibleWeaponIds = Array.Empty<string>();
        public AttachmentModifiers modifiers = new AttachmentModifiers();
        public GameObject visualPrefab;
        public bool suppressesFireSound;
        public string ItemId => item != null ? item.Id : string.Empty;
        public string DisplayName => item != null ? item.DisplayName : name;

        public bool IsCompatible(WeaponDefinition weapon)
        {
            if (weapon == null || !Enum.IsDefined(typeof(AttachmentSlot), slot) ||
                (weapon.supportedAttachmentSlots & (AttachmentSlots)(1 << (int)slot)) == 0) return false;
            var weaponClass = weapon.weaponClass == WeaponClass.Rifle ? AttachmentWeaponClasses.Rifle : AttachmentWeaponClasses.Pistol;
            if ((compatibleClasses & weaponClass) == 0) return false;
            return compatibleWeaponIds == null || compatibleWeaponIds.Length == 0 || Array.IndexOf(compatibleWeaponIds, weapon.WeaponId) >= 0;
        }
        public void Validate()
        {
            if (item == null || item.Type != ItemType.Attachment) throw new ArgumentException("Attachment needs an existing Attachment ItemDefinition.");
            item.Validate();
            if (!Enum.IsDefined(typeof(AttachmentSlot), slot) || compatibleClasses == 0 || modifiers == null || !modifiers.IsValid)
                throw new ArgumentException("Invalid attachment configuration: " + name);
        }
    }
}
