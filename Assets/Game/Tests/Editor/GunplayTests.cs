using NUnit.Framework;
using Outbreak.Weapons;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace Outbreak.Tests
{
    // Non-rendering regressions against the saved environment: no Play Mode or screenshots.
    public sealed class GunplaySceneTests
    {
        private Scene scene;
        private FirearmController firearm;
        private SerializedObject fields;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        [SetUp] public void OpenScene()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/Game/Scenes/Gunplay_Test.unity");
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent(out FirearmController controller)) firearm = controller;
            Assert.IsNotNull(firearm);
            fields = new SerializedObject(firearm);
        }
        [TearDown] public void CloseScene()
        {
            fields?.Dispose();
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }
        private T Reference<T>(string field) where T : Object => fields.FindProperty(field).objectReferenceValue as T;

        [Test] public void SavedLoadoutHasPersistentDefinitionsAndMatchingViews()
        {
            var primary = Reference<WeaponDefinition>("primary");
            var secondary = Reference<WeaponDefinition>("secondary");
            Assert.IsNotNull(primary, "Primary must survive scene serialization.");
            Assert.IsNotNull(secondary, "Secondary must survive scene serialization.");
            Assert.IsTrue(AssetDatabase.Contains(primary)); Assert.IsTrue(AssetDatabase.Contains(secondary));
            Assert.DoesNotThrow(primary.Validate); Assert.DoesNotThrow(secondary.Validate);
            Assert.AreEqual(WeaponClass.Rifle, primary.weaponClass);
            Assert.AreEqual(WeaponClass.Pistol, secondary.weaponClass);
            Assert.AreEqual("DebugRifle", Reference<WeaponViewModel>("primaryView").name);
            Assert.AreEqual("DebugPistol", Reference<WeaponViewModel>("secondaryView").name);
        }

        [Test] public void ControllerShowsOnlySelectedRuntimeStateAndKeepsAmmo()
        {
            typeof(FirearmController).GetMethod("EquipDefinitions", PrivateInstance).Invoke(firearm,
                new object[] { Reference<WeaponDefinition>("primary"), Reference<WeaponDefinition>("secondary"), 180, 75 });
            var selection = (WeaponSelection)typeof(FirearmController).GetField("selection", PrivateInstance).GetValue(firearm);
            var rifle = Reference<WeaponViewModel>("primaryView");
            var pistol = Reference<WeaponViewModel>("secondaryView");
            var show = typeof(FirearmController).GetMethod("ShowViews", PrivateInstance);
            Assert.IsTrue(rifle.gameObject.activeSelf); Assert.IsFalse(pistol.gameObject.activeSelf);
            selection.Primary.ProcessTrigger(true, 0);
            Assert.IsTrue(selection.Select(false, 1));
            Assert.AreSame(pistol, show.Invoke(firearm, new object[] { firearm.Current }));
            Assert.IsFalse(rifle.gameObject.activeSelf); Assert.IsTrue(pistol.gameObject.activeSelf);
            selection.Tick(2); selection.Secondary.ProcessTrigger(false, 2);
            selection.Secondary.ProcessTrigger(true, 3);
            Assert.IsTrue(selection.Select(true, 4)); selection.Tick(5);
            Assert.AreSame(rifle, show.Invoke(firearm, new object[] { firearm.Current }));
            Assert.IsTrue(rifle.gameObject.activeSelf); Assert.IsFalse(pistol.gameObject.activeSelf);
            Assert.AreEqual(29, selection.Primary.AmmoInMagazine);
            Assert.AreEqual(14, selection.Secondary.AmmoInMagazine);
            Assert.AreEqual(180, selection.Primary.ReserveAmmo); Assert.AreEqual(75, selection.Secondary.ReserveAmmo);
            show.Invoke(firearm, new object[] { null });
            Assert.IsFalse(rifle.gameObject.activeSelf); Assert.IsFalse(pistol.gameObject.activeSelf);
        }

        [TestCase("primaryView")] [TestCase("secondaryView")]
        public void AdsRemainsCameraLocalThroughLeanAndCrouch(string field)
        {
            var camera = Reference<Camera>("viewCamera");
            var view = Reference<WeaponViewModel>(field);
            Assert.AreSame(camera.transform, view.transform.parent.parent);
            Assert.AreEqual(Vector3.zero, view.transform.parent.localPosition);
            Assert.AreEqual(Quaternion.identity, view.transform.parent.localRotation);
            for (var t = view.transform; t != null; t = t.parent) Assert.AreEqual(Vector3.one, t.localScale);
            Assert.AreEqual(0.04f, camera.nearClipPlane, 0.0001f);
            foreach (var renderer in view.GetComponentsInChildren<Renderer>(true))
                Assert.AreNotEqual(0, camera.cullingMask & (1 << renderer.gameObject.layer));
            Assert.IsEmpty(view.GetComponentsInChildren<Collider>(true));
            var rig = camera.transform.parent;
            var lookRoot = rig.parent;
            Assert.AreEqual("BodycamRig", rig.name); Assert.AreEqual("ViewRoot", lookRoot.name);
            foreach (float height in new[] { 1.66f, 1.01f })
            foreach (float lean in new[] { -1f, 0f, 1f })
            {
                lookRoot.localPosition = Vector3.up * height;
                lookRoot.localRotation = Quaternion.Euler(35, 2, 0);
                rig.localPosition = new Vector3(lean * 0.18f, 0.008f, 0);
                rig.localRotation = Quaternion.Euler(0.3f, -0.4f, -lean * 7);
                var parentPosition = rig.localPosition; var parentRotation = rig.localRotation;
                view.UpdatePose(1, false, false, 1);
                var tip = camera.transform.InverseTransformPoint(view.transform.Find("Front sight").TransformPoint(Vector3.up * 0.5f));
                Assert.AreEqual(0, tip.x, 0.0001f); Assert.AreEqual(0, tip.y, 0.0001f);
                Assert.Greater(tip.z, camera.nearClipPlane);
                view.Kick(0.035f, 4); view.UpdatePose(1, false, false, 1f / 60);
                Assert.Greater(camera.transform.InverseTransformPoint(view.transform.position).z, camera.nearClipPlane);
                Assert.AreEqual(parentPosition, rig.localPosition); Assert.AreEqual(parentRotation, rig.localRotation);
            }
        }
    }

    public sealed class GunplayTests
    {
        private WeaponDefinition definition;
        [SetUp] public void Setup()
        {
            definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.magazineCapacity = 30; definition.fireRate = 600; definition.reloadDuration = 2;
        }
        [TearDown] public void Teardown() => Object.DestroyImmediate(definition);
        [Test] public void EmptyMagazineProducesOnlyDryFire()
        {
            var state = new WeaponRuntimeState(definition, 50, 0);
            int fired = 0, dry = 0;
            state.OnWeaponFired += () => fired++; state.OnDryFire += () => dry++;
            Assert.AreEqual(FireResult.DryFire, state.ProcessTrigger(true, 0));
            Assert.AreEqual(0, fired); Assert.AreEqual(1, dry); Assert.AreEqual(0, state.AmmoInMagazine);
        }
        [Test] public void ShotConsumesExactlyOneAndEmitsOnce()
        {
            var state = new WeaponRuntimeState(definition, 50); int changed = 0;
            state.OnAmmoChanged += () => changed++;
            Assert.AreEqual(FireResult.Fired, state.ProcessTrigger(true, 0));
            Assert.AreEqual(29, state.AmmoInMagazine); Assert.AreEqual(50, state.ReserveAmmo); Assert.AreEqual(1, changed);
        }
        [Test] public void SemiAutoRequiresRelease()
        {
            var state = new WeaponRuntimeState(definition, 0);
            state.ProcessTrigger(true, 0);
            Assert.AreEqual(FireResult.None, state.ProcessTrigger(true, 1));
            state.ProcessTrigger(false, 1.1);
            Assert.AreEqual(FireResult.Fired, state.ProcessTrigger(true, 1.2));
        }
        [Test] public void SemiAutoCannotBypassCadenceByClicking()
        {
            var state = new WeaponRuntimeState(definition, 0);
            state.ProcessTrigger(true, 0); state.ProcessTrigger(false, 0.01);
            Assert.AreEqual(FireResult.None, state.ProcessTrigger(true, 0.02));
            Assert.AreEqual(29, state.AmmoInMagazine);
        }
        [Test] public void AutomaticHeldRespectsCadenceAndDoesNotCatchUp()
        {
            definition.fireMode = FireMode.Automatic;
            var state = new WeaponRuntimeState(definition, 0);
            Assert.AreEqual(FireResult.Fired, state.ProcessTrigger(true, 0));
            Assert.AreEqual(FireResult.None, state.ProcessTrigger(true, 0.05));
            Assert.AreEqual(FireResult.Fired, state.ProcessTrigger(true, 0.101));
            Assert.AreEqual(FireResult.Fired, state.ProcessTrigger(true, 20));
            Assert.AreEqual(27, state.AmmoInMagazine);
        }
        [TestCase(50, 30, 31)] [TestCase(8, 19, 0)]
        public void ReloadTransfersOnlyNeededAmmo(int reserve, int expectedMagazine, int expectedReserve)
        {
            var state = new WeaponRuntimeState(definition, reserve, 11);
            Assert.IsTrue(state.StartReload(10)); state.Tick(11.99);
            Assert.AreEqual(11, state.AmmoInMagazine);
            state.Tick(12); state.Tick(100);
            Assert.AreEqual(expectedMagazine, state.AmmoInMagazine); Assert.AreEqual(expectedReserve, state.ReserveAmmo);
            Assert.AreEqual(11 + reserve, state.AmmoInMagazine + state.ReserveAmmo);
        }
        [Test] public void ReloadWithoutReserveIsRejected()
        { Assert.IsFalse(new WeaponRuntimeState(definition, 0, 11).StartReload(0)); }
        [Test] public void FullMagazineCannotReload()
        { Assert.IsFalse(new WeaponRuntimeState(definition, 50).StartReload(0)); }
        [Test] public void ReloadBlocksFireAndDuplicateReload()
        {
            var state = new WeaponRuntimeState(definition, 50, 11); state.StartReload(0);
            Assert.IsFalse(state.StartReload(0.1));
            Assert.AreEqual(FireResult.None, state.ProcessTrigger(true, 0.5)); Assert.AreEqual(11, state.AmmoInMagazine);
        }
        [Test] public void CancelledReloadTransfersNothing()
        {
            var state = new WeaponRuntimeState(definition, 50, 11); state.StartReload(0); state.CancelReload(); state.Tick(10);
            Assert.AreEqual(11, state.AmmoInMagazine); Assert.AreEqual(50, state.ReserveAmmo);
        }
        [Test] public void MantleBlocksAndConsumesSemiAutoPress()
        {
            var state = new WeaponRuntimeState(definition, 0);
            Assert.AreEqual(FireResult.None, state.ProcessTrigger(true, 0, mantling: true));
            Assert.AreEqual(FireResult.None, state.ProcessTrigger(true, 1));
            Assert.AreEqual(30, state.AmmoInMagazine);
        }
        [TestCase(true, FireResult.None)] [TestCase(false, FireResult.Fired)]
        public void SprintUsesDefinitionRestriction(bool restriction, FireResult expected)
        {
            definition.sprintRestriction = restriction;
            Assert.AreEqual(expected, new WeaponRuntimeState(definition, 0).ProcessTrigger(true, 0, sprinting: true));
        }
        [Test] public void SwitchPreservesEachMagazineAndBlocksFire()
        {
            var primary = new WeaponRuntimeState(definition, 50); var secondary = new WeaponRuntimeState(definition, 20);
            var selection = new WeaponSelection(primary, secondary, 0.3f);
            primary.ProcessTrigger(true, 0);
            Assert.IsTrue(selection.Select(false, 1));
            secondary.ProcessTrigger(false, 1.05);
            Assert.AreEqual(FireResult.None, secondary.ProcessTrigger(true, 1.1, switching: selection.IsSwitching));
            selection.Tick(1.31); secondary.ProcessTrigger(false, 1.4); secondary.ProcessTrigger(true, 1.5);
            selection.Select(true, 2); selection.Tick(2.31);
            Assert.AreSame(primary, selection.Current); Assert.AreEqual(29, primary.AmmoInMagazine); Assert.AreEqual(29, secondary.AmmoInMagazine);
            Assert.AreEqual(50, primary.ReserveAmmo); Assert.AreEqual(20, secondary.ReserveAmmo);
        }
        [Test] public void SwitchCancelsReloadWithoutAmmoTransfer()
        {
            var primary = new WeaponRuntimeState(definition, 50, 11);
            var selection = new WeaponSelection(primary, new WeaponRuntimeState(definition, 0), 0.3f);
            primary.StartReload(0); selection.Select(false, 0.5); primary.Tick(10);
            Assert.IsFalse(primary.IsReloading); Assert.AreEqual(11, primary.AmmoInMagazine);
        }
        [TestCase(HitZone.Body, 28)] [TestCase(HitZone.Head, 56)] [TestCase(HitZone.Limb, 28)]
        public void DamageUsesHitZone(HitZone zone, float expected)
        {
            var info = new DamageInfo(definition, zone, Vector3.one, Vector3.up, null);
            Assert.AreEqual(expected, info.Damage); Assert.AreEqual(zone == HitZone.Head, info.IsHeadshot);
        }
        [Test] public void ModeSwitchOnlyForSupportingWeapons()
        {
            var state = new WeaponRuntimeState(definition, 0); state.ToggleFireMode();
            Assert.AreEqual(FireMode.SemiAutomatic, state.CurrentFireMode);
            definition.allowModeSwitch = true; state.ToggleFireMode(); Assert.AreEqual(FireMode.Automatic, state.CurrentFireMode);
        }
        [Test] public void ReloadEventsAndAimAreConsistent()
        {
            var state = new WeaponRuntimeState(definition, 10, 11); int starts = 0, ends = 0;
            state.OnReloadStarted += () => starts++; state.OnReloadCompleted += () => ends++;
            state.SetAim(true); state.StartReload(0); state.SetAim(true);
            Assert.IsFalse(state.IsAiming); state.Tick(2); state.Tick(3);
            Assert.AreEqual(1, starts); Assert.AreEqual(1, ends);
        }
    }
}
