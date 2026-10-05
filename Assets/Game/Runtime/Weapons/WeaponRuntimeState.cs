using System;
using System.Collections.Generic;
using UnityEngine;

namespace Outbreak.Weapons
{
    public enum FireResult { None, Fired, DryFire }

    // No Unity clock/input or mutable ScriptableObject data: callers supply time and action gates.
    public sealed class WeaponRuntimeState
    {
        public WeaponDefinition Definition { get; }
        public AttachmentSet Attachments { get; } = new AttachmentSet();
        public EffectiveWeaponStats Stats { get; private set; }
        public float EffectiveNoiseMultiplier => Stats.NoiseMultiplier;
        public int AmmoInMagazine { get; private set; }
        public int ReserveAmmo { get; private set; }
        public bool IsReloading { get; private set; }
        public bool IsAiming { get; private set; }
        public FireMode CurrentFireMode { get; private set; }
        public double NextFireTime { get; private set; }
        public double ReloadEndTime { get; private set; }
        public double ReloadStartTime { get; private set; }
        private bool triggerWasHeld;
        public event Action OnWeaponFired;
        public event Action OnDryFire;
        public event Action OnReloadStarted;
        public event Action OnReloadCompleted;
        public event Action OnAmmoChanged;
        public event Action<bool> OnAimChanged;
        public event Action OnAttachmentsChanged;

        public WeaponRuntimeState(WeaponDefinition definition, int reserveAmmo, int magazine = -1)
        {
            Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            Stats = new EffectiveWeaponStats(definition, Attachments);
            AmmoInMagazine = magazine < 0 ? Stats.MagazineCapacity : Mathf.Clamp(magazine, 0, Stats.MagazineCapacity);
            ReserveAmmo = Mathf.Max(0, reserveAmmo);
            CurrentFireMode = definition.fireMode;
        }
        public FireResult ProcessTrigger(bool held, double now, bool mantling = false, bool sprinting = false, bool switching = false, bool obstructed = false)
        {
            bool pressed = held && !triggerWasHeld;
            triggerWasHeld = held; // Blocked presses are consumed, never queued for semi-auto.
            if (!held || (CurrentFireMode == FireMode.SemiAutomatic && !pressed) || IsReloading || mantling ||
                switching || obstructed || (sprinting && Definition.sprintRestriction) || now < NextFireTime) return FireResult.None;
            NextFireTime = now + Definition.ShotInterval; // Dry fire is also rate limited.
            if (AmmoInMagazine == 0) { OnDryFire?.Invoke(); return FireResult.DryFire; }
            AmmoInMagazine--;
            OnAmmoChanged?.Invoke();
            OnWeaponFired?.Invoke();
            return FireResult.Fired;
        }
        public bool StartReload(double now)
        {
            if (IsReloading || AmmoInMagazine >= Stats.MagazineCapacity || ReserveAmmo == 0) return false;
            SetAim(false);
            IsReloading = true;
            ReloadStartTime = now;
            ReloadEndTime = now + Stats.ReloadDuration;
            OnReloadStarted?.Invoke();
            return true;
        }
        public void Tick(double now)
        {
            if (!IsReloading || now < ReloadEndTime) return;
            int transfer = Math.Min(Math.Max(0, Stats.MagazineCapacity - AmmoInMagazine), ReserveAmmo);
            AmmoInMagazine += transfer;
            ReserveAmmo -= transfer;
            IsReloading = false;
            OnAmmoChanged?.Invoke();
            OnReloadCompleted?.Invoke();
        }
        public void CancelReload() => IsReloading = false;
        public void SetAim(bool value)
        {
            value &= !IsReloading;
            if (value == IsAiming) return;
            IsAiming = value;
            OnAimChanged?.Invoke(value);
        }
        public void ToggleFireMode()
        {
            if (Definition.allowModeSwitch && !IsReloading)
                CurrentFireMode = CurrentFireMode == FireMode.Automatic ? FireMode.SemiAutomatic : FireMode.Automatic;
        }
        public void Unequip() { CancelReload(); SetAim(false); triggerWasHeld = true; }
        public void SuppressHeldTrigger() => triggerWasHeld = true;

        public bool TryEquipAttachment(AttachmentDefinition attachment)
        {
            if (IsReloading || attachment == null || !attachment.IsCompatible(Definition)) return false;
            try { attachment.Validate(); } catch (ArgumentException) { return false; }
            if (Attachments[attachment.slot] == attachment) return true;
            Attachments.Set(attachment.slot, attachment);
            RefreshAttachments(); return true;
        }
        public bool TryRemoveAttachment(AttachmentSlot slot)
        {
            if (IsReloading || !Enum.IsDefined(typeof(AttachmentSlot), slot) || Attachments[slot] == null) return false;
            Attachments.Set(slot, null); RefreshAttachments(); return true;
        }
        public bool TryApplyAttachments(IEnumerable<AttachmentDefinition> definitions)
        {
            if (IsReloading || definitions == null) return false;
            var proposed = new AttachmentSet();
            foreach (var entry in definitions)
            {
                if (entry == null || !entry.IsCompatible(Definition) || proposed[entry.slot] != null) return false;
                try { entry.Validate(); } catch (ArgumentException) { return false; }
                proposed.Set(entry.slot, entry);
            }
            Attachments.Replace(proposed); RefreshAttachments(); return true;
        }
        private void RefreshAttachments()
        {
            Stats = new EffectiveWeaponStats(Definition, Attachments);
            int overflow = Math.Max(0, AmmoInMagazine - Stats.MagazineCapacity);
            AmmoInMagazine -= overflow; ReserveAmmo += overflow;
            OnAttachmentsChanged?.Invoke();
            OnAmmoChanged?.Invoke();
        }
    }

    // Two existing loadout slots, not a second inventory. Runtime state survives switching.
    public sealed class WeaponSelection
    {
        public WeaponRuntimeState Primary { get; }
        public WeaponRuntimeState Secondary { get; }
        public WeaponRuntimeState Current { get; private set; }
        public bool IsSwitching { get; private set; }
        public float SwitchDuration { get; }
        private double switchEnd;
        public float SwitchProgress(double now) => !IsSwitching || SwitchDuration <= 0 ? 1 : Mathf.Clamp01((float)(1 - (switchEnd - now) / SwitchDuration));
        public event Action<WeaponRuntimeState> OnWeaponEquipped;
        public WeaponSelection(WeaponRuntimeState primary, WeaponRuntimeState secondary, float duration)
        { Primary = primary; Secondary = secondary; Current = primary ?? secondary; SwitchDuration = Mathf.Max(0, duration); }
        public bool Select(bool primary, double now)
        {
            var next = primary ? Primary : Secondary;
            if (next == null || next == Current || IsSwitching) return false;
            Current?.Unequip();
            Current = next;
            Current.SuppressHeldTrigger();
            IsSwitching = true;
            switchEnd = now + SwitchDuration;
            return true;
        }
        public void Tick(double now)
        {
            if (!IsSwitching || now < switchEnd) return;
            IsSwitching = false;
            OnWeaponEquipped?.Invoke(Current);
        }
        public void CancelActions() { Current?.Unequip(); IsSwitching = false; }
    }
}
