using System;
using UnityEngine;

namespace Outbreak.Infected
{
    public enum InfectedState { Idle, Wander, Investigate, Chase, Attack, Stagger, Dead }
    [Serializable]
    public sealed class InfectedSettings
    {
        [Min(0.1f)] public float wanderSpeed = 0.8f, chaseSpeed = 2.4f;
        [Min(0.1f)] public float wanderRadius = 4;
        [Min(0.1f)] public float visionRange = 18, hearingRange = 35;
        [Range(10, 180)] public float visionAngle = 105;
        [Min(0.05f)] public float perceptionInterval = 0.15f, destinationInterval = 0.3f;
        [Min(0)] public float reactionTime = 0.25f, loseSightDelay = 2.5f;
        [Min(0.1f)] public float searchDuration = 4, investigationTimeout = 12;
        [Min(0.1f)] public float idleMinimum = 1.5f, idleMaximum = 3.5f;
        [Min(0.1f)] public float attackRange = 1.35f, attackWindup = 0.45f, attackCooldown = 1.35f;
        [Min(0)] public float attackDamage = 15;
        [Min(0)] public float staggerThreshold = 20, staggerDuration = 0.3f, staggerCooldown = 1.5f;
    }
    public readonly struct InfectedObservation
    {
        public readonly bool Visible, TargetAlive, Arrived;
        public readonly float Distance;
        public InfectedObservation(bool visible, bool targetAlive, float distance, bool arrived)
        { Visible = visible; TargetAlive = targetAlive; Distance = distance; Arrived = arrived; }
    }
    // Decision/timing only. Controller owns perception, NavMesh and melee LOS validation.
    public sealed class InfectedBrain
    {
        private readonly InfectedSettings settings;
        private double idleUntil, lastSeen = double.NegativeInfinity, seenSince = double.NaN;
        private double entered, searchUntil = double.NaN, hitAt, nextAttack, staggerUntil, nextStagger;
        private float heardImportance;
        private bool attackPending;
        public InfectedState State { get; private set; } = InfectedState.Idle;
        public event Action<InfectedState> OnStateChanged;
        public event Action OnStrike;
        public InfectedBrain(InfectedSettings settings, double now = 0)
        { this.settings = settings; idleUntil = now + settings.idleMinimum; }
        private void Enter(InfectedState state, double now)
        {
            if (state == State) return;
            State = state; entered = now; searchUntil = double.NaN; attackPending = false;
            OnStateChanged?.Invoke(state);
        }
        public void WaitUntil(double until) { idleUntil = until; }
        public bool Hear(float importance, double now)
        {
            if (State == InfectedState.Dead || State == InfectedState.Chase || State == InfectedState.Attack || State == InfectedState.Stagger) return false;
            if (State == InfectedState.Investigate && importance <= heardImportance && now - entered < 3) return false;
            heardImportance = importance; Enter(InfectedState.Investigate, now);
            entered = now; searchUntil = double.NaN; return true;
        }
        public bool Stagger(float damage, double now)
        {
            if (State == InfectedState.Dead || damage < settings.staggerThreshold || now < nextStagger) return false;
            nextStagger = now + Math.Max(settings.staggerCooldown, settings.staggerDuration + 0.1f);
            staggerUntil = now + settings.staggerDuration; Enter(InfectedState.Stagger, now); return true;
        }
        public void Die(double now) { Enter(InfectedState.Dead, now); }
        public void Tick(double now, InfectedObservation observation)
        {
            if (State == InfectedState.Dead) return;
            bool sees = observation.Visible && observation.TargetAlive;
            if (sees) { lastSeen = now; if (double.IsNaN(seenSince)) seenSince = now; }
            else seenSince = double.NaN;
            if (State == InfectedState.Stagger)
            {
                if (now < staggerUntil) return;
                Enter(sees ? InfectedState.Chase : InfectedState.Investigate, now);
            }
            if (sees && now - seenSince >= settings.reactionTime && State != InfectedState.Attack && State != InfectedState.Chase)
                Enter(InfectedState.Chase, now);
            switch (State)
            {
                case InfectedState.Idle:
                    if (now >= idleUntil) Enter(InfectedState.Wander, now);
                    break;
                case InfectedState.Wander:
                    if (observation.Arrived || now - entered > settings.investigationTimeout) GoIdle(now);
                    break;
                case InfectedState.Investigate:
                    if (observation.Arrived && double.IsNaN(searchUntil)) searchUntil = now + settings.searchDuration;
                    if ((!double.IsNaN(searchUntil) && now >= searchUntil) || now - entered > settings.investigationTimeout) GoIdle(now);
                    break;
                case InfectedState.Chase:
                    if (!observation.TargetAlive) { GoIdle(now); break; }
                    if (sees && observation.Distance <= settings.attackRange) Enter(InfectedState.Attack, now);
                    else if (!sees && now - lastSeen >= settings.loseSightDelay) Enter(InfectedState.Investigate, now);
                    break;
                case InfectedState.Attack:
                    if (!sees || observation.Distance > settings.attackRange) { Enter(InfectedState.Chase, now); break; }
                    if (!attackPending && now >= nextAttack)
                    { attackPending = true; hitAt = now + settings.attackWindup; }
                    if (attackPending && now >= hitAt)
                    {
                        attackPending = false; nextAttack = now + settings.attackCooldown;
                        OnStrike?.Invoke();
                    }
                    break;
            }
        }
        private void GoIdle(double now) { idleUntil = now + settings.idleMinimum; heardImportance = 0; Enter(InfectedState.Idle, now); }
    }
}
