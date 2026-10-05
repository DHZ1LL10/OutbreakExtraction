using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Outbreak.Combat;
using Outbreak.Core;
using Outbreak.Infected;
using Outbreak.Items;
using Outbreak.Maps;
using Outbreak.Persistence;
using Outbreak.Weapons;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Outbreak.Tests
{
    public sealed class InfectedCombatTests
    {
        private readonly List<Object> objects = new List<Object>();
        private string directory;
        private static DamageInfo Damage(float amount) => new DamageInfo(amount, Vector3.zero, Vector3.up, null);
        private T Component<T>() where T : Component
        { var go = new GameObject(typeof(T).Name); objects.Add(go); return go.AddComponent<T>(); }
        private T Asset<T>() where T : ScriptableObject
        { var asset = ScriptableObject.CreateInstance<T>(); objects.Add(asset); return asset; }
        [TearDown] public void Cleanup()
        {
            foreach (var value in objects) Object.DestroyImmediate(value); objects.Clear();
            if (directory != null && Directory.Exists(directory))
            {
                // A GUID-named folder created by this test only, under Unity's temporary cache.
                foreach (var file in Directory.GetFiles(directory)) File.Delete(file);
                Directory.Delete(directory); directory = null;
            }
        }
        [Test] public void PlayerDamageAndHealAreClampedAndEmitEvents()
        {
            var health = Component<PlayerHealth>(); int damaged = 0, healed = 0;
            health.OnDamaged += _ => damaged++; health.OnHealed += _ => healed++;
            Assert.AreEqual(100, health.CurrentHealth);
            health.ApplyDamage(Damage(25)); Assert.AreEqual(75, health.CurrentHealth);
            health.Heal(200); Assert.AreEqual(100, health.CurrentHealth);
            health.Heal(10); health.Heal(-1); health.ApplyDamage(Damage(-10));
            Assert.AreEqual(1, damaged); Assert.AreEqual(1, healed);
        }
        [Test] public void PlayerDeathHappensOnceAndHealingCannotResurrect()
        {
            var health = Component<PlayerHealth>(); int deaths = 0;
            health.OnDied += _ => deaths++;
            health.ApplyDamage(Damage(1000)); health.ApplyDamage(Damage(10)); health.Heal(100);
            Assert.AreEqual(0, health.CurrentHealth); Assert.IsFalse(health.IsAlive); Assert.AreEqual(1, deaths);
        }
        [Test] public void InfectedReceivesAlreadyCalculatedHeadshotExactlyOnce()
        {
            var health = Component<InfectedHealth>(); var weapon = Asset<WeaponDefinition>();
            weapon.damage = 24; weapon.headshotMultiplier = 2;
            health.ApplyDamage(new DamageInfo(weapon, HitZone.Head, Vector3.zero, Vector3.up, null));
            Assert.AreEqual(52, health.CurrentHealth); // 100 - 48, not 100 - 96.
        }
        [Test] public void InfectedIgnoresDamageAfterDeath()
        {
            var health = Component<InfectedHealth>(); int hits = 0, deaths = 0;
            health.OnDamaged += _ => hits++; health.OnDied += _ => deaths++;
            health.ApplyDamage(Damage(30)); health.ApplyDamage(Damage(80)); health.ApplyDamage(Damage(50));
            Assert.AreEqual(0, health.CurrentHealth); Assert.AreEqual(2, hits); Assert.AreEqual(1, deaths);
        }
        [Test] public void HealthRejectsNonFiniteValues()
        {
            var health = new HealthState(100); health.ApplyDamage(Damage(float.NaN)); health.Heal(float.PositiveInfinity);
            Assert.AreEqual(100, health.CurrentHealth); Assert.Throws<ArgumentOutOfRangeException>(() => new HealthState(float.NaN));
        }
        private static InfectedSettings Settings() => new InfectedSettings { reactionTime = 0, attackWindup = 0.4f, attackCooldown = 1, staggerDuration = 0.3f, staggerCooldown = 2 };
        private static InfectedObservation Seen(float distance = 5) => new InfectedObservation(true, true, distance, false);
        private static InfectedObservation Hidden(bool arrived = false) => new InfectedObservation(false, true, 10, arrived);
        [Test] public void IdleInvestigatesNoiseThenChasesVisiblePlayer()
        {
            var brain = new InfectedBrain(Settings());
            Assert.IsTrue(brain.Hear(10, 0)); Assert.AreEqual(InfectedState.Investigate, brain.State);
            brain.Tick(0.1, Seen()); Assert.AreEqual(InfectedState.Chase, brain.State);
        }
        [Test] public void ChaseEntersAttackOnlyInRange()
        {
            var brain = new InfectedBrain(Settings()); brain.Tick(0, Seen()); Assert.AreEqual(InfectedState.Chase, brain.State);
            brain.Tick(0.1, Seen(1)); Assert.AreEqual(InfectedState.Attack, brain.State);
        }
        [Test] public void AttackRespectsWindupCooldownAndDoesNotCatchUp()
        {
            var brain = new InfectedBrain(Settings()); int strikes = 0; brain.OnStrike += () => strikes++;
            brain.Tick(0, Seen(1)); brain.Tick(0.01, Seen(1)); brain.Tick(0.2, Seen(1)); Assert.AreEqual(0, strikes);
            brain.Tick(0.42, Seen(1)); Assert.AreEqual(1, strikes);
            brain.Tick(1.3, Seen(1)); Assert.AreEqual(1, strikes);
            brain.Tick(1.43, Seen(1)); brain.Tick(1.84, Seen(1)); Assert.AreEqual(2, strikes);
            brain.Tick(100, Seen(1)); Assert.AreEqual(2, strikes); // A new windup, no burst of missed attacks.
        }
        [Test] public void LosingLineOfSightCancelsPendingStrike()
        {
            var brain = new InfectedBrain(Settings()); int strikes = 0; brain.OnStrike += () => strikes++;
            brain.Tick(0, Seen(1)); brain.Tick(0.01, Seen(1)); brain.Tick(0.5, Hidden());
            Assert.AreEqual(0, strikes); Assert.AreEqual(InfectedState.Chase, brain.State);
        }
        [Test] public void MemoryPersistsThenInvestigatesAndReturnsToIdle()
        {
            var brain = new InfectedBrain(Settings()); brain.Tick(0, Seen()); brain.Tick(1, Hidden());
            Assert.AreEqual(InfectedState.Chase, brain.State);
            brain.Tick(3, Hidden()); Assert.AreEqual(InfectedState.Investigate, brain.State);
            brain.Tick(3.1, Hidden(true)); brain.Tick(7.2, Hidden(true)); Assert.AreEqual(InfectedState.Idle, brain.State);
        }
        [Test] public void DeadCannotHearStaggerOrAttack()
        {
            var brain = new InfectedBrain(Settings()); int strikes = 0; brain.OnStrike += () => strikes++;
            brain.Tick(0, Seen(1)); brain.Tick(0.01, Seen(1)); brain.Die(0.2); brain.Tick(10, Seen(1));
            Assert.AreEqual(InfectedState.Dead, brain.State); Assert.AreEqual(0, strikes);
            Assert.IsFalse(brain.Hear(100, 11)); Assert.IsFalse(brain.Stagger(100, 11));
        }
        [Test] public void StaggerEndsAndCooldownPreventsPermanentStunlock()
        {
            var brain = new InfectedBrain(Settings()); brain.Tick(0, Seen());
            Assert.IsTrue(brain.Stagger(24, 0.1)); brain.Tick(0.2, Seen()); Assert.AreEqual(InfectedState.Stagger, brain.State);
            Assert.IsFalse(brain.Stagger(24, 0.3)); brain.Tick(0.5, Seen()); Assert.AreEqual(InfectedState.Chase, brain.State);
            Assert.IsFalse(brain.Stagger(24, 1)); Assert.IsTrue(brain.Stagger(24, 2.2));
        }
        [Test] public void MinorDamageDoesNotStagger()
        { Assert.IsFalse(new InfectedBrain(Settings()).Stagger(5, 0)); }
        [Test] public void StrongerNoiseCanReplaceInvestigationButWeakNoiseCannot()
        {
            var brain = new InfectedBrain(Settings()); Assert.IsTrue(brain.Hear(10, 0));
            Assert.IsFalse(brain.Hear(2, 1)); Assert.IsTrue(brain.Hear(20, 1));
        }
        [Test] public void NoiseOnlyReachesListenersInsideBothRadii()
        {
            var bus = new NoiseBus(); int near = 0, far = 0, limited = 0;
            var subscription = bus.Subscribe(() => new Vector3(3, 0, 0), 20, _ => near++);
            bus.Subscribe(() => new Vector3(11, 0, 0), 20, _ => far++);
            bus.Subscribe(() => new Vector3(3, 0, 0), 2, _ => limited++);
            bus.Emit(new NoiseEvent(Vector3.zero, 10, null)); Assert.AreEqual(1, near); Assert.AreEqual(0, far); Assert.AreEqual(0, limited);
            subscription.Dispose(); bus.Emit(new NoiseEvent(Vector3.zero, 10, null)); Assert.AreEqual(1, near);
        }
        [Test] public void SuppressedShotReachesFewerListeners()
        {
            float normal = NoiseRules.ShotRadius(WeaponClass.Rifle, 1), suppressed = NoiseRules.ShotRadius(WeaponClass.Rifle, 0.25f);
            Assert.AreEqual(40, normal); Assert.AreEqual(10, suppressed);
            var bus = new NoiseBus(); int heard = 0; bus.Subscribe(() => new Vector3(20, 0, 0), 35, _ => heard++);
            bus.Emit(new NoiseEvent(Vector3.zero, suppressed, null)); Assert.AreEqual(0, heard);
            bus.Emit(new NoiseEvent(Vector3.zero, normal, null)); Assert.AreEqual(1, heard);
        }
        [Test] public void SlowAndCrouchNoiseAreLowerThanSprint()
        {
            Assert.Less(NoiseRules.MovementRadius(false, true, true), NoiseRules.MovementRadius(false, true, false));
            Assert.Less(NoiseRules.MovementRadius(false, true, false), NoiseRules.MovementRadius(false, false, false));
            Assert.Less(NoiseRules.MovementRadius(false, false, false), NoiseRules.MovementRadius(true, false, false));
        }
        [Test] public void ZombieStrikeDamagesPlayerHealthThroughExistingContract()
        {
            var settings = Settings(); var brain = new InfectedBrain(settings); var player = Component<PlayerHealth>();
            brain.OnStrike += () => ((IDamageable)player).ApplyDamage(Damage(settings.attackDamage));
            brain.Tick(0, Seen(1)); brain.Tick(0.01, Seen(1)); brain.Tick(0.42, Seen(1));
            Assert.AreEqual(85, player.CurrentHealth);
        }
        [Test] public void PlayerDeathStartsAndCompletesOneSequence()
        {
            var player = Component<PlayerHealth>(); var sequence = new DeathLifecycle(3);
            int starts = 0, finishes = 0; sequence.OnStarted += () => starts++; sequence.OnCompleted += () => finishes++;
            player.OnDied += _ => sequence.TryBegin();
            player.ApplyDamage(Damage(100)); player.ApplyDamage(Damage(100)); Assert.IsFalse(sequence.TryBegin());
            sequence.Advance(2); Assert.AreEqual(0, finishes); sequence.Advance(1); sequence.Advance(10);
            Assert.AreEqual(1, starts); Assert.AreEqual(1, finishes);
        }
        [Test] public void RaidFailureRunsOnceAfterDeathAndNeverFailsANewSession()
        {
            var catalog = Asset<ItemCatalog>(); var map = Asset<RaidMapDefinition>();
            var so = new UnityEditor.SerializedObject(map); so.FindProperty("id").stringValue = "infected_test"; so.ApplyModifiedPropertiesWithoutUndo();
            directory = Path.Combine(Application.temporaryCachePath, "InfectedTests_" + Guid.NewGuid().ToString("N"));
            var flow = new GameFlow(catalog, new SaveService(directory)); Assert.IsTrue(flow.StartRaid(map));
            var completion = new RaidDeathCompletion(flow); Assert.IsTrue(completion.Complete()); Assert.AreEqual(GameState.RaidFailed, flow.State);
            Assert.IsFalse(completion.Complete()); flow.ReturnToHub(); Assert.IsTrue(flow.StartRaid(map));
            Assert.IsFalse(completion.Complete()); Assert.AreEqual(GameState.Raid, flow.State);
            var stale = new RaidDeathCompletion(flow); flow.FailRaid(); flow.ReturnToHub(); flow.StartRaid(map);
            Assert.IsFalse(stale.Complete()); Assert.AreEqual(GameState.Raid, flow.State);
        }
        [Test] public void DebugDeathWithoutRaidCompletesSafely()
        { Assert.IsFalse(new RaidDeathCompletion(null).Complete()); }
    }
}
