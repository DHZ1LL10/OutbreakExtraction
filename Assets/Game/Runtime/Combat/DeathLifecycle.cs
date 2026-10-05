using System;
using Outbreak.Core;
using Outbreak.Raids;

namespace Outbreak.Combat
{
    public sealed class DeathLifecycle
    {
        private readonly float duration;
        private float elapsed;
        public bool Started { get; private set; }
        public bool Completed { get; private set; }
        public float Progress => Math.Min(1, elapsed / duration);
        public event Action OnStarted;
        public event Action OnCompleted;
        public DeathLifecycle(float seconds) { duration = Math.Max(0.1f, seconds); }
        public bool TryBegin()
        {
            if (Started) return false;
            Started = true; OnStarted?.Invoke(); return true;
        }
        public void Advance(float dt)
        {
            if (!Started || Completed || dt <= 0 || float.IsNaN(dt)) return;
            elapsed += dt;
            if (elapsed < duration) return;
            Completed = true; OnCompleted?.Invoke();
        }
    }
    // Capture the deployed session at death start; never fail a different/new session later.
    public sealed class RaidDeathCompletion
    {
        private readonly GameFlow flow;
        private readonly RaidSession session;
        private bool handled;
        public RaidDeathCompletion(GameFlow flow) { this.flow = flow; session = flow?.Session; }
        public bool Complete()
        {
            if (handled) return false;
            handled = true;
            return session != null && session.IsActive && flow.Session == session && flow.State == GameState.Raid && flow.FailRaid();
        }
    }
}
