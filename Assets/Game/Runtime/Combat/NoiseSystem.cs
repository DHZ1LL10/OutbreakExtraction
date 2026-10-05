using System;
using System.Collections.Generic;
using Outbreak.Weapons;
using UnityEngine;

namespace Outbreak.Combat
{
    public readonly struct NoiseEvent
    {
        public Vector3 Position { get; }
        public float Radius { get; }
        public GameObject Source { get; }
        public NoiseEvent(Vector3 position, float radius, GameObject source)
        { Position = position; Radius = radius; Source = source; }
    }
    public sealed class NoiseBus
    {
        private sealed class Listener : IDisposable
        {
            private readonly NoiseBus owner;
            public Listener(NoiseBus owner) { this.owner = owner; }
            public Func<Vector3> Position;
            public float HearingRange;
            public Action<NoiseEvent> Receive;
            public bool Active = true;
            public void Dispose() { Active = false; owner.listeners.Remove(this); }
        }
        private readonly List<Listener> listeners = new List<Listener>();
        public IDisposable Subscribe(Func<Vector3> position, float hearingRange, Action<NoiseEvent> receive)
        {
            if (position == null || receive == null) throw new ArgumentNullException();
            var listener = new Listener(this) { Position = position, HearingRange = Mathf.Max(0, hearingRange), Receive = receive };
            listeners.Add(listener); return listener;
        }
        public void Emit(NoiseEvent noise)
        {
            if (noise.Radius <= 0 || float.IsNaN(noise.Radius) || float.IsInfinity(noise.Radius)) return;
            // Per-noise snapshot (not per frame); callbacks can safely subscribe/dispose/emit.
            foreach (var listener in listeners.ToArray())
            {
                float radius = Mathf.Min(noise.Radius, listener.HearingRange);
                if (listener.Active && (listener.Position() - noise.Position).sqrMagnitude <= radius * radius) listener.Receive(noise);
            }
        }
    }
    public static class NoiseSystem
    {
        public static NoiseBus Bus { get; private set; } = new NoiseBus();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Bus = new NoiseBus(); }
        public static void EmitNoise(Vector3 position, float radius, GameObject source) => Bus.Emit(new NoiseEvent(position, radius, source));
    }
    public static class NoiseRules
    {
        public static float ShotRadius(WeaponClass weapon, float multiplier, float rifleRadius = 40, float pistolRadius = 24)
            => Mathf.Max(0, (weapon == WeaponClass.Rifle ? rifleRadius : pistolRadius) * multiplier);
        public static float MovementRadius(bool sprint, bool slow, bool crouch)
            => sprint ? 12 : crouch && slow ? 0.8f : slow ? 2 : crouch ? 3 : 5;
    }
}
