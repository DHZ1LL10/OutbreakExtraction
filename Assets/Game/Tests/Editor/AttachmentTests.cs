using System.Collections.Generic;
using NUnit.Framework;
using Outbreak.Items;
using Outbreak.Weapons;
using UnityEditor;
using UnityEngine;

namespace Outbreak.Tests
{
    public sealed class AttachmentTests
    {
        private readonly List<Object> objects = new List<Object>();
        private WeaponDefinition rifle, pistol;
        private AttachmentDefinition optic, suppressor, compensator, magazine, grip;
        private T Make<T>() where T : ScriptableObject
        { var value = ScriptableObject.CreateInstance<T>(); objects.Add(value); return value; }
        private ItemDefinition Item(string id, ItemType type)
        {
            var item = Make<ItemDefinition>(); var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = id; so.FindProperty("displayName").stringValue = id;
            so.FindProperty("type").enumValueIndex = (int)type; so.ApplyModifiedPropertiesWithoutUndo(); so.Dispose(); return item;
        }
        private AttachmentDefinition Attachment(string id, AttachmentSlot slot)
        {
            var attachment = Make<AttachmentDefinition>(); attachment.item = Item(id, ItemType.Attachment); attachment.slot = slot;
            return attachment;
        }
        [SetUp] public void Setup()
        {
            rifle = Make<WeaponDefinition>(); rifle.item = Item("rifle", ItemType.Weapon); rifle.weaponClass = WeaponClass.Rifle;
            rifle.magazineCapacity = 30; rifle.recoilVertical = 2; rifle.recoilHorizontal = 1;
            rifle.hipSpread = 4; rifle.adsSpread = 1; rifle.adsSpeed = 10; rifle.reloadDuration = 2;
            pistol = Make<WeaponDefinition>(); pistol.item = Item("pistol", ItemType.Weapon); pistol.magazineCapacity = 15;
            optic = Attachment("red_dot", AttachmentSlot.Optic); optic.modifiers.adsSpread = new StatModifier(0.85f);
            suppressor = Attachment("suppressor", AttachmentSlot.Muzzle); suppressor.suppressesFireSound = true;
            suppressor.modifiers.noise = new StatModifier(0.25f); suppressor.modifiers.muzzleFlash = new StatModifier(0.12f);
            compensator = Attachment("compensator", AttachmentSlot.Muzzle); compensator.modifiers.recoilVertical = new StatModifier(0.72f);
            compensator.modifiers.noise = new StatModifier(1.2f); compensator.modifiers.muzzleFlash = new StatModifier(1.3f);
            magazine = Attachment("extended", AttachmentSlot.Magazine); magazine.modifiers.magazineCapacity = new StatModifier(1.5f);
            magazine.modifiers.reloadDuration = new StatModifier(1.1f);
            grip = Attachment("grip", AttachmentSlot.Grip); grip.compatibleClasses = AttachmentWeaponClasses.Rifle;
            grip.modifiers.recoilVertical = new StatModifier(0.85f); grip.modifiers.recoilHorizontal = new StatModifier(0.8f);
            grip.modifiers.hipSpread = new StatModifier(0.9f); grip.modifiers.adsSpeed = new StatModifier(0.92f);
        }
        [TearDown] public void TearDown()
        { foreach (var value in objects) Object.DestroyImmediate(value); objects.Clear(); }

        [Test] public void UnsupportedSlotIsRejected()
        {
            rifle.supportedAttachmentSlots = AttachmentSlots.Muzzle;
            var state = new WeaponRuntimeState(rifle, 0);
            Assert.IsFalse(state.TryEquipAttachment(optic)); Assert.IsNull(state.Attachments[AttachmentSlot.Optic]);
            Assert.IsTrue(state.TryEquipAttachment(suppressor));
        }
        [Test] public void ClassAndItemIdCompatibilityAreEnforced()
        {
            Assert.IsFalse(new WeaponRuntimeState(pistol, 0).TryEquipAttachment(grip));
            optic.compatibleWeaponIds = new[] { "rifle" };
            Assert.IsTrue(new WeaponRuntimeState(rifle, 0).TryEquipAttachment(optic));
            Assert.IsFalse(new WeaponRuntimeState(pistol, 0).TryEquipAttachment(optic));
        }
        [Test] public void ExistingItemIdentityIsRequired()
        {
            optic.item = rifle.item;
            Assert.IsFalse(new WeaponRuntimeState(rifle, 0).TryEquipAttachment(optic));
        }
        [Test] public void EquippingSameSlotReplacesItsPreviousAttachment()
        {
            var state = new WeaponRuntimeState(rifle, 0);
            state.TryEquipAttachment(suppressor); state.TryEquipAttachment(compensator);
            Assert.AreSame(compensator, state.Attachments[AttachmentSlot.Muzzle]);
            Assert.AreEqual(1, new List<AttachmentDefinition>(state.Attachments.Equipped).Count);
            Assert.AreEqual(1.2f, state.Stats.NoiseMultiplier, 0.0001f);
        }
        [Test] public void RemoveRestoresBaseStatsAndKeepsDefinitionUnchanged()
        {
            var state = new WeaponRuntimeState(rifle, 0); state.TryEquipAttachment(compensator);
            Assert.AreEqual(2, rifle.recoilVertical); Assert.IsTrue(state.TryRemoveAttachment(AttachmentSlot.Muzzle));
            Assert.AreEqual(2, state.Stats.RecoilVertical); Assert.IsFalse(state.TryRemoveAttachment(AttachmentSlot.Muzzle));
        }
        [Test] public void CompensatorReducesEffectiveVerticalRecoil()
        {
            var state = new WeaponRuntimeState(rifle, 0); state.TryEquipAttachment(compensator);
            Assert.AreEqual(1.44f, state.Stats.RecoilVertical, 0.0001f);
            Assert.AreEqual(1.3f, state.Stats.MuzzleFlashMultiplier, 0.0001f);
        }
        [Test] public void GripChangesBothRecoilAxesAndAdsSpeed()
        {
            var state = new WeaponRuntimeState(rifle, 0); state.TryEquipAttachment(grip);
            Assert.AreEqual(1.7f, state.Stats.RecoilVertical, 0.0001f);
            Assert.AreEqual(0.8f, state.Stats.RecoilHorizontal, 0.0001f);
            Assert.AreEqual(9.2f, state.Stats.AdsSpeed, 0.0001f);
        }
        [Test] public void EffectiveHipAndAdsSpreadUseSeparateModifiers()
        {
            var state = new WeaponRuntimeState(rifle, 0); state.TryEquipAttachment(optic); state.TryEquipAttachment(grip);
            Assert.AreEqual(3.6f, state.Stats.HipSpread, 0.0001f); Assert.AreEqual(0.85f, state.Stats.AdsSpread, 0.0001f);
        }
        [Test] public void AdditivesSumBeforeMultipliersAndEquipOrderDoesNotMatter()
        {
            compensator.modifiers.recoilVertical = new StatModifier(0.72f, 0.5f);
            grip.modifiers.recoilVertical = new StatModifier(0.85f, 0.25f);
            var first = new WeaponRuntimeState(rifle, 0); var second = new WeaponRuntimeState(rifle, 0);
            first.TryEquipAttachment(compensator); first.TryEquipAttachment(grip);
            second.TryEquipAttachment(grip); second.TryEquipAttachment(compensator);
            Assert.AreEqual(2.75f * 0.72f * 0.85f, first.Stats.RecoilVertical, 0.0001f);
            Assert.AreEqual(first.Stats.RecoilVertical, second.Stats.RecoilVertical);
        }
        [TestCase(true, 45)] [TestCase(false, 22)]
        public void ExtendedCapacityDoesNotCreateAmmo(bool useRifle, int capacity)
        {
            var definition = useRifle ? rifle : pistol; var state = new WeaponRuntimeState(definition, 50);
            int total = state.AmmoInMagazine + state.ReserveAmmo;
            Assert.IsTrue(state.TryEquipAttachment(magazine)); Assert.AreEqual(capacity, state.Stats.MagazineCapacity);
            Assert.AreEqual(definition.magazineCapacity, state.AmmoInMagazine);
            Assert.AreEqual(total, state.AmmoInMagazine + state.ReserveAmmo);
        }
        [TestCase(true)] [TestCase(false)] public void RemovingExtendedMagazineReturnsOverflow(bool useRifle)
        {
            var definition = useRifle ? rifle : pistol; var state = new WeaponRuntimeState(definition, 50);
            int total = state.AmmoInMagazine + state.ReserveAmmo;
            state.TryEquipAttachment(magazine); state.StartReload(0); state.Tick(10);
            Assert.AreEqual(state.Stats.MagazineCapacity, state.AmmoInMagazine);
            Assert.IsTrue(state.TryRemoveAttachment(AttachmentSlot.Magazine));
            Assert.AreEqual(definition.magazineCapacity, state.AmmoInMagazine);
            Assert.AreEqual(50, state.ReserveAmmo); Assert.AreEqual(total, state.AmmoInMagazine + state.ReserveAmmo);
        }
        [Test] public void RepeatedMagazineTogglesCannotDuplicateAmmo()
        {
            var state = new WeaponRuntimeState(rifle, 50, 17);
            for (int i = 0; i < 30; i++)
            {
                state.TryEquipAttachment(magazine); state.StartReload(i * 20); state.Tick(i * 20 + 10);
                state.TryRemoveAttachment(AttachmentSlot.Magazine);
                Assert.AreEqual(67, state.AmmoInMagazine + state.ReserveAmmo);
            }
        }
        [Test] public void ReloadUsesEffectiveDurationAndBlocksConfigurationChanges()
        {
            var state = new WeaponRuntimeState(rifle, 50); state.TryEquipAttachment(magazine);
            Assert.IsTrue(state.StartReload(0)); Assert.AreEqual(2.2, state.ReloadEndTime, 0.0001);
            Assert.IsFalse(state.TryRemoveAttachment(AttachmentSlot.Magazine)); Assert.IsFalse(state.TryEquipAttachment(optic));
            Assert.IsFalse(state.TryApplyAttachments(new AttachmentDefinition[0]));
            state.Tick(2.19); Assert.AreEqual(30, state.AmmoInMagazine);
            state.Tick(2.21); Assert.AreEqual(45, state.AmmoInMagazine); Assert.AreEqual(35, state.ReserveAmmo);
        }
        [Test] public void SuppressorExposesNoiseAndFlashWithoutANoiseSystem()
        {
            var state = new WeaponRuntimeState(rifle, 0); state.TryEquipAttachment(suppressor);
            Assert.AreEqual(0.25f, state.EffectiveNoiseMultiplier, 0.0001f);
            Assert.AreEqual(0.12f, state.Stats.MuzzleFlashMultiplier, 0.0001f);
        }
        [Test] public void RifleAndPistolKeepIndependentSetsAcrossSelection()
        {
            var primary = new WeaponRuntimeState(rifle, 50); var secondary = new WeaponRuntimeState(pistol, 20);
            primary.TryEquipAttachment(suppressor); secondary.TryEquipAttachment(compensator);
            var firstSet = primary.Attachments; var secondSet = secondary.Attachments;
            var selection = new WeaponSelection(primary, secondary, 0.3f);
            selection.Select(false, 1); selection.Tick(2);
            Assert.AreSame(secondSet, selection.Current.Attachments);
            selection.Current.TryEquipAttachment(optic);
            selection.Select(true, 3); selection.Tick(4);
            Assert.AreSame(firstSet, selection.Current.Attachments); Assert.IsNull(firstSet[AttachmentSlot.Optic]);
            Assert.AreSame(suppressor, firstSet[AttachmentSlot.Muzzle]); Assert.AreSame(compensator, secondSet[AttachmentSlot.Muzzle]);
        }
        [Test] public void BulkApplyIsAtomicAndExportsOnlyExistingItemIds()
        {
            var state = new WeaponRuntimeState(rifle, 50); state.TryEquipAttachment(suppressor);
            Assert.IsFalse(state.TryApplyAttachments(new[] { optic, suppressor, compensator }));
            Assert.IsNull(state.Attachments[AttachmentSlot.Optic]); Assert.AreSame(suppressor, state.Attachments[AttachmentSlot.Muzzle]);
            Assert.IsTrue(state.TryApplyAttachments(new[] { optic, magazine }));
            CollectionAssert.AreEquivalent(new[] { optic.item.Id, magazine.item.Id }, state.Attachments.ExportItemIds());
            Assert.IsNull(state.Attachments[AttachmentSlot.Muzzle]); Assert.AreEqual(30, state.AmmoInMagazine);
        }
        [Test] public void InvalidNumericModifiersAreRejectedWithoutChangingState()
        {
            var state = new WeaponRuntimeState(rifle, 0); optic.modifiers.adsSpread = new StatModifier(float.NaN);
            Assert.IsFalse(state.TryEquipAttachment(optic)); Assert.AreEqual(1, state.Stats.AdsSpread);
        }
        [TestCase(WeaponAudioCue.Fire)] [TestCase(WeaponAudioCue.DryFire)]
        [TestCase(WeaponAudioCue.Reload)] [TestCase(WeaponAudioCue.Equip)]
        public void NullAudioClipsAreSilentAndDoNotThrow(WeaponAudioCue cue)
        {
            var state = new WeaponRuntimeState(rifle, 0);
            var go = new GameObject("Null clip test"); objects.Add(go); var source = go.AddComponent<AudioSource>();
            Assert.IsNull(WeaponAudio.Resolve(state, cue));
            Assert.DoesNotThrow(() => WeaponAudio.PlayOptional(source, WeaponAudio.Resolve(state, cue)));
            Assert.DoesNotThrow(() => WeaponAudio.PlayOptional(null, null)); Assert.IsFalse(source.isPlaying);
            state.TryEquipAttachment(suppressor); Assert.IsNull(WeaponAudio.Resolve(state, WeaponAudioCue.Fire));
        }
        [Test] public void ObstructionConsumesSemiPressWithoutAmmoOrQueuedShot()
        {
            var state = new WeaponRuntimeState(rifle, 0);
            Assert.AreEqual(FireResult.None, state.ProcessTrigger(true, 0, obstructed: true));
            Assert.AreEqual(FireResult.None, state.ProcessTrigger(true, 1)); Assert.AreEqual(30, state.AmmoInMagazine);
            state.ProcessTrigger(false, 2); Assert.AreEqual(FireResult.Fired, state.ProcessTrigger(true, 3));
        }
    }
}
