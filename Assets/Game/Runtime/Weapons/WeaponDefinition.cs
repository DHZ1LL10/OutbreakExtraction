using System;
using Outbreak.Items;
using UnityEngine;

namespace Outbreak.Weapons
{
    public enum WeaponClass { Pistol, Rifle }
    public enum FireMode { SemiAutomatic, Automatic }

    // Item identity/economy/persistence remain owned by the existing ItemDefinition.
    [CreateAssetMenu(menuName = "Outbreak/Weapons/Firearm")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public ItemDefinition item;
        public ItemDefinition ammoType;
        public WeaponClass weaponClass;
        public AttachmentSlots supportedAttachmentSlots = AttachmentSlots.All;
        [Min(0)] public float noiseMultiplier = 1;
        [Min(0)] public float muzzleFlashMultiplier = 1;
        [Header("Optional audio; null clips are silent")]
        public AudioClip fireSound, suppressedFireSound, dryFireSound, reloadSound, equipSound;
        [Min(0.1f)] public float damage = 28;
        [Min(1)] public float headshotMultiplier = 2;
        [Tooltip("Rounds per minute"), Min(1)] public float fireRate = 360;
        public FireMode fireMode = FireMode.SemiAutomatic;
        public bool allowModeSwitch;
        [Min(1)] public int magazineCapacity = 15;
        [Min(0.01f)] public float reloadDuration = 1.65f;
        [Min(1)] public float range = 100;
        [Tooltip("Cone half-angle in degrees"), Min(0)] public float hipSpread = 1;
        [Min(0)] public float adsSpread = 0.08f;
        [Min(0)] public float recoilVertical = 1.25f;
        [Min(0)] public float recoilHorizontal = 0.18f;
        [Min(0.1f)] public float recoilRecovery = 5;
        [Range(30, 90)] public float adsFov = 58;
        [Min(0.1f)] public float adsSpeed = 12;
        [Min(1)] public float movementSpreadMultiplier = 1.5f;
        [Range(0.1f, 1)] public float crouchSpreadMultiplier = 0.7f;
        [Min(1)] public float airSpreadMultiplier = 2;
        public bool sprintRestriction = true;
        [Min(0)] public float visualKick = 0.035f;
        [Min(0)] public float visualRotation = 4;
        [Min(0)] public float cameraKick = 0.15f;
        public string WeaponId => item != null ? item.Id : string.Empty;
        public string DisplayName => item != null ? item.DisplayName : name;
        public float ShotInterval => 60f / Mathf.Max(1, fireRate);
        public void Validate()
        {
            if (item == null || item.Type != ItemType.Weapon) throw new ArgumentException("Firearm needs a Weapon item.");
            item.Validate();
            if (ammoType == null || ammoType.Type != ItemType.Ammo) throw new ArgumentException("Firearm needs an Ammo item.");
            ammoType.Validate();
            if (magazineCapacity < 1 || fireRate <= 0 || reloadDuration <= 0 || range <= 0 || damage <= 0 ||
                !Enum.IsDefined(typeof(FireMode), fireMode)) throw new ArgumentException("Invalid firearm settings.");
        }
    }
}
