using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Outbreak.Cameras;
using Outbreak.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Outbreak.Tests
{
    public sealed class FPSControllerTests
    {
        private GameObject root, player;
        private PlayerInput input;
        private PlayerMotor motor;
        private PlayerLook look;
        private BodycamController bodycam;
        private CharacterController controller;
        private readonly Vector3 origin = new Vector3(2000, 20, 2000);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject("FPS physics test");
            Box("Floor", origin - Vector3.up * 0.25f, new Vector3(10, 0.5f, 10));
            player = new GameObject("Player under test");
            player.SetActive(false);
            player.transform.SetParent(root.transform);
            player.transform.position = origin + Vector3.up * 0.03f;
            controller = player.AddComponent<CharacterController>();
            controller.radius = 0.3f;
            controller.skinWidth = 0.03f;
            controller.stepOffset = 0.28f;
            input = player.AddComponent<PlayerInput>();
            input.SetControlEnabled(false);
            look = player.AddComponent<PlayerLook>();
            motor = player.AddComponent<PlayerMotor>();
            var view = new GameObject("ViewRoot").transform;
            view.SetParent(player.transform, false);
            var rig = new GameObject("BodycamRig").transform;
            rig.SetParent(view, false);
            var cameraObject = new GameObject("Non-rendering test camera");
            cameraObject.transform.SetParent(rig, false);
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.enabled = false;
            bodycam = rig.gameObject.AddComponent<BodycamController>();
            Set(look, "viewRoot", view);
            Set(motor, "viewRoot", view);
            Set(bodycam, "motor", motor);
            Set(bodycam, "look", look);
            Set(bodycam, "input", input);
            Set(bodycam, "viewCamera", camera);
            player.SetActive(true);
            Physics.SyncTransforms();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StandingClearanceRejectsRoofButIgnoresTriggersAndSelf()
        {
            motor.SetMotorEnabled(false);
            controller.height = 1.15f;
            controller.center = Vector3.up * 0.575f;
            Assert.IsTrue(motor.CanStand());
            var roof = Box("Roof", origin + Vector3.up * 1.47f, new Vector3(3, 0.3f, 3));
            Physics.SyncTransforms();
            Assert.IsFalse(motor.CanStand());
            roof.GetComponent<Collider>().isTrigger = true;
            Physics.SyncTransforms();
            Assert.IsTrue(motor.CanStand());
            roof.GetComponent<Collider>().isTrigger = false;
            roof.transform.SetParent(player.transform, true);
            Physics.SyncTransforms();
            Assert.IsTrue(motor.CanStand());
            yield return null;
        }

        [UnityTest]
        public IEnumerator FallingReportsOneLandingWithImpactSpeed()
        {
            controller.enabled = false;
            player.transform.position = origin + Vector3.up * 3;
            controller.enabled = true;
            Physics.SyncTransforms();
            int count = 0;
            float impact = 0;
            motor.OnLand += value => { count++; impact = value; };
            float deadline = Time.time + 3;
            while (count == 0 && Time.time < deadline) yield return null;
            Assert.AreEqual(1, count);
            Assert.Greater(impact, 6);
            Assert.IsTrue(motor.IsGrounded);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(1, count, "Resting on the floor must not repeatedly fire landing events.");
        }

        [UnityTest]
        public IEnumerator DeathHandoffDisablesNormalControlAndRetainsExternalPose()
        {
            Vector3 before = bodycam.transform.position;
            bodycam.SetDeathPose(before + Vector3.one, Quaternion.identity);
            Assert.AreEqual(before, bodycam.transform.position);
            int events = 0;
            bodycam.OnDeathStateEntered += () => events++;
            bodycam.EnterDeathState();
            bodycam.EnterDeathState();
            var position = origin + new Vector3(0.1f, 0.15f, 0.2f);
            var rotation = Quaternion.Euler(15, 30, 70);
            bodycam.SetDeathPose(position, rotation);
            yield return null;
            yield return null;
            Assert.AreEqual(1, events);
            Assert.IsFalse(input.ControlEnabled);
            Assert.IsFalse(look.enabled);
            Assert.AreEqual(Vector3.zero, motor.Velocity);
            Assert.Less(Vector3.Distance(position, bodycam.transform.position), 0.001f);
            Assert.Less(Quaternion.Angle(rotation, bodycam.transform.rotation), 0.01f);
        }

        private GameObject Box(string name, Vector3 position, Vector3 scale)
        {
            var box = new GameObject(name);
            box.transform.SetParent(root.transform);
            box.transform.position = position;
            box.transform.localScale = scale;
            box.AddComponent<BoxCollider>();
            return box;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CrouchedTunnelEntryPreservesForwardAndLateralSpeed(bool lateral)
        {
            ManualMotor();
            Crouch();
            Box("Tunnel roof", origin + new Vector3(0, 1.47f, 1), new Vector3(3, 0.3f, 4));
            Teleport(origin + new Vector3(0, 0.03f, -2));
            player.transform.rotation = Quaternion.Euler(0, lateral ? 90 : 0, 0);
            InputValue("MoveInput", lateral ? Vector2.left : Vector2.up);
            Physics.SyncTransforms();
            float minimumSpeed = float.PositiveInfinity;
            for (int i = 0; i < 110; i++)
            {
                Tick();
                if (i > 20) minimumSpeed = Mathf.Min(minimumSpeed, motor.Speed);
            }
            Assert.Greater(minimumSpeed, 1.45f, "Valid roof clearance must not erase horizontal momentum.");
            Assert.Greater(player.transform.position.z - origin.z, 1.25f);
            Assert.IsFalse(motor.CanStand());
            Assert.Less(controller.stepOffset, 0.17f, "Automatic step lift must fit below the ceiling.");
        }

        [Test]
        public void SlowWalkUsesStanceSpeedsAndSprintTakesPriority()
        {
            ManualMotor();
            InputValue("MoveInput", Vector2.up);
            InputValue("SlowWalkHeld", true);
            Tick(25);
            Assert.IsTrue(motor.IsWalkingSlow);
            Assert.That(motor.Speed, Is.EqualTo(1.4f).Within(0.08f));
            InputValue("SprintHeld", true);
            Tick(20);
            Assert.IsTrue(motor.IsSprinting);
            Assert.IsFalse(motor.IsWalkingSlow);
            Assert.That(motor.Speed, Is.EqualTo(5.4f).Within(0.08f));
            Crouch();
            Tick(20);
            Assert.IsFalse(motor.IsSprinting);
            Assert.IsTrue(motor.IsWalkingSlow);
            Assert.That(motor.Speed, Is.EqualTo(0.75f).Within(0.08f));
            input.SetControlEnabled(false);
            Tick();
            Assert.IsFalse(motor.IsWalkingSlow);
            Assert.AreEqual(0, input.LeanInput);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void JumpKeepsLaunchMomentumAndHasWeightedHeight(bool sprint)
        {
            ManualMotor();
            Teleport(origin + new Vector3(0, 0.03f, -4));
            InputValue("MoveInput", Vector2.up);
            InputValue("SprintHeld", sprint);
            Tick(20);
            float launchSpeed = motor.Speed;
            float startY = player.transform.position.y;
            float startZ = player.transform.position.z;
            InputValue("JumpPressed", true);
            Tick();
            InputValue("JumpPressed", false);
            InputValue("SprintHeld", false);
            float apex = player.transform.position.y;
            int frames = 0;
            while (!motor.IsGrounded && frames++ < 100)
            {
                Tick();
                apex = Mathf.Max(apex, player.transform.position.y);
                if (!motor.IsGrounded) Assert.That(motor.Speed, Is.EqualTo(launchSpeed).Within(0.1f));
            }
            Assert.IsTrue(motor.IsGrounded);
            Assert.That(apex - startY, Is.InRange(0.95f, 1.15f));
            Assert.That(player.transform.position.z - startZ, Is.InRange(sprint ? 2.8f : 1.6f, sprint ? 3.5f : 2.1f));
            InputValue("JumpPressed", true);
            Tick();
            Assert.IsTrue(motor.IsGrounded, "Landing recovery must reject an immediate second jump.");
        }

        [TestCase(0.18f, false, true)]
        [TestCase(0.8f, false, true)]
        [TestCase(1.0f, false, true)]
        [TestCase(2.4f, false, false)]
        [TestCase(0.8f, true, false)]
        public void ContextualMantleRequiresLowSupportedSurfaceAndClearCapsule(float height, bool roof, bool expected)
        {
            ManualMotor();
            Box("Mantle obstacle", origin + new Vector3(0, height * 0.5f, 1.5f), new Vector3(2, height, 2));
            if (roof) Box("Blocked destination", origin + new Vector3(0, 1.85f, 1.5f), new Vector3(2, 0.3f, 2));
            Physics.SyncTransforms();
            Tick(10);
            InputValue("JumpPressed", true);
            Tick();
            InputValue("JumpPressed", false);
            Assert.AreEqual(expected, motor.IsMantling);
            if (!expected) return;
            InputValue("MoveInput", Vector2.left);
            Tick(30);
            Assert.IsFalse(motor.IsMantling);
            Assert.That(player.transform.position.y - origin.y, Is.EqualTo(height).Within(0.08f));
            Assert.Greater(player.transform.position.z - origin.z, 0.8f);
            // The final six ticks are normal movement, proving control was restored.
            Assert.Less(player.transform.position.x - origin.x, -0.05f);
        }

        [Test]
        public void MantleCrossesThinLowBarrierOntoSupportedGround()
        {
            ManualMotor();
            Box("Thin barrier", origin + new Vector3(0, 0.35f, 0.65f), new Vector3(2, 0.7f, 0.25f));
            Physics.SyncTransforms();
            Tick(10);
            InputValue("JumpPressed", true);
            Tick();
            InputValue("JumpPressed", false);
            Assert.IsTrue(motor.IsMantling);
            Tick(30);
            Assert.IsFalse(motor.IsMantling);
            Assert.Greater(player.transform.position.z - origin.z, 1.1f);
            Assert.That(player.transform.position.y - origin.y, Is.EqualTo(0).Within(0.08f));
        }

        [Test]
        public void MantleCancelsWhenNewObstacleBlocksItsPath()
        {
            ManualMotor();
            Box("Mantle box", origin + new Vector3(0, 0.4f, 1.5f), new Vector3(2, 0.8f, 2));
            Physics.SyncTransforms();
            Tick(10);
            InputValue("JumpPressed", true);
            Tick();
            InputValue("JumpPressed", false);
            Assert.IsTrue(motor.IsMantling);
            Box("Moving blocker", origin + new Vector3(0, 1.9f, 0), new Vector3(2, 0.2f, 2));
            Physics.SyncTransforms();
            Tick(30);
            Assert.IsFalse(motor.IsMantling);
            Assert.Less(player.transform.position.z - origin.z, 0.4f);
            Assert.IsTrue(controller.enabled);
        }

        [Test]
        public void LeanCollisionClampsCameraTravelAndIgnoresTriggers()
        {
            motor.SetMotorEnabled(false);
            var rig = bodycam.transform;
            Vector3 neutral = rig.parent.TransformPoint(Vector3.zero);
            var wall = Box("Side wall", neutral + Vector3.right * 0.23f, new Vector3(0.1f, 3, 3));
            Physics.SyncTransforms();
            var method = typeof(BodycamController).GetMethod("CameraTravelFraction", BindingFlags.Instance | BindingFlags.NonPublic);
            float blocked = (float)method.Invoke(bodycam, new object[] { Vector3.right * 0.18f });
            Assert.That(blocked, Is.InRange(0.1f, 0.4f));
            wall.GetComponent<Collider>().isTrigger = true;
            Physics.SyncTransforms();
            Assert.AreEqual(1, (float)method.Invoke(bodycam, new object[] { Vector3.right * 0.18f }));
        }

        private void ManualMotor()
        {
            input.enabled = false;
            // These synchronous tests advance the motor within one Unity frame. Keep it enabled
            // so native CharacterController contact callbacks participate in each Move.
            bodycam.enabled = false;
        }
        private void Crouch()
        {
            controller.height = 1.15f;
            controller.center = Vector3.up * 0.575f;
            Property(motor, "IsCrouching", true);
        }
        private void Teleport(Vector3 position)
        {
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
        }
        private void InputValue(string name, object value) => Property(input, name, value);
        private static void Property(object target, string name, object value) =>
            target.GetType().GetProperty(name).GetSetMethod(true).Invoke(target, new[] { value });
        private void Tick(int frames = 1)
        {
            var method = typeof(PlayerMotor).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < frames; i++) method.Invoke(motor, new object[] { 0.02f });
        }
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
