using System;
using System.Collections.Generic;
using Outbreak.Combat;
using Outbreak.Core;
using Outbreak.Inventory;
using Outbreak.Player;
using UnityEngine;

namespace Outbreak.Raids
{
    // Timing is independent of keys, raycasts and rendering.
    public sealed class ExtractionCountdown
    {
        public float Duration { get; }
        public bool Running { get; private set; }
        public bool Completed { get; private set; }
        private double startedAt;
        public ExtractionCountdown(float duration) { Duration = Mathf.Max(0.1f, duration); }
        public float Remaining(double now) => Running ? Mathf.Max(0, Duration - (float)(now - startedAt)) : Duration;
        public void Cancel() { Running = false; }
        public bool Tick(double now, bool inside, bool alive, bool activeRaid)
        {
            if (Completed) return false;
            if (!inside || !alive || !activeRaid) { Cancel(); return false; }
            if (!Running) { Running = true; startedAt = now; }
            if (Remaining(now) > 0) return false;
            Completed = true; Running = false; return true;
        }
    }

    [RequireComponent(typeof(Collider)), DisallowMultipleComponent]
    public sealed class ExtractionZone : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float extractionDuration = 5f;
        [SerializeField] private string displayName = "Extraction";
        [SerializeField] private RaidRuntimeContext raidContext;
        [SerializeField] private bool logTransitions;
        private readonly List<ItemStack> extracted = new List<ItemStack>();
        private readonly HashSet<Collider> occupants = new HashSet<Collider>();
        private ExtractionCountdown countdown;
        private PlayerHealth player;
        private RaidSession countingSession;
        private float cancelledUntil;
        private double lastTickAt;
        public bool Succeeded { get; private set; }
        public bool IsCountingDown => countdown != null && countdown.Running;
        public float Progress => IsCountingDown ? Mathf.Clamp01(1 - Remaining / extractionDuration) : (Succeeded ? 1 : 0);
        public float Remaining => Succeeded ? 0 : countdown != null ? countdown.Remaining(lastTickAt) : extractionDuration;
        public bool CancelledRecently => !Succeeded && Time.unscaledTime < cancelledUntil;
        public float CompletedAt { get; private set; }
        public float RaidDuration { get; private set; }
        public IReadOnlyList<ItemStack> Extracted => extracted.AsReadOnly();
        public int EstimatedValue { get; private set; }
        public string DisplayName => displayName;
        private void Awake()
        {
            ConfigureTrigger(null);
            countdown = new ExtractionCountdown(extractionDuration);
            if (raidContext == null) raidContext = FindFirstObjectByType<RaidRuntimeContext>();
            // Upgrade the old generated label without touching other scene objects.
            foreach (var label in FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                if (label.text == "EXTRACTION \u2014 HOLD E") label.gameObject.SetActive(false);
        }
        // Legacy scenes used the thin floor marker as their trigger. Preserve its footprint and renderer,
        // but raise the trigger volume above the floor so a CharacterController genuinely enters it.
        public void ConfigureTrigger(RaidRuntimeContext context)
        {
            if (raidContext == null && context != null) raidContext = context;
            var trigger = GetComponent<Collider>(); trigger.enabled = true; trigger.isTrigger = true;
            if (trigger is BoxCollider box)
            {
                float scaleY = Mathf.Max(0.0001f, Mathf.Abs(box.transform.lossyScale.y));
                if (box.size.y * scaleY < 0.5f)
                {
                    float bottom = box.center.y - box.size.y * 0.5f;
                    var size = box.size; size.y = 2.4f / scaleY; box.size = size;
                    var center = box.center; center.y = bottom + size.y * 0.5f; box.center = center;
                }
            }
        }
        private void LogTransition(string message) { if (logTransitions) Debug.Log("[Raid] Extraction " + message, this); }
        private void Update() => Tick(Time.timeAsDouble);
        public void Tick(double now)
        {
            lastTickAt = now;
            if (Succeeded) return;
            occupants.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            bool inside = player != null && player.isActiveAndEnabled && occupants.Count > 0;
            var flow = raidContext?.Flow;
            var session = flow?.Session;
            bool active = flow?.State == GameState.Raid && session?.IsActive == true;
            if (IsCountingDown && (countingSession != session || !inside || !player.IsAlive || !active)) Cancel();
            if (!inside || !player.IsAlive || !active) return;
            countingSession = session;
            bool wasRunning = IsCountingDown;
            bool completed = countdown.Tick(now, inside, player.IsAlive, active);
            if (!wasRunning && IsCountingDown) LogTransition("started");
            if (!completed) return;
            extracted.Clear(); EstimatedValue = 0;
            foreach (var stack in session.FoundLoot)
            {
                extracted.Add(stack);
                EstimatedValue = checked(EstimatedValue + stack.Item.SellValue * stack.Quantity);
            }
            if (!flow.ExtractSuccessfully())
            {
                extracted.Clear(); EstimatedValue = 0;
                countdown = new ExtractionCountdown(extractionDuration); cancelledUntil = Time.unscaledTime + 1.5f;
                Debug.LogWarning("[Raid] Extraction failed: " + flow.LastMessage, this); return;
            }
            Succeeded = true; CompletedAt = Time.unscaledTime; RaidDuration = raidContext.ElapsedRaidTime;
            LogTransition("completed");
            player.GetComponent<PlayerInput>()?.SetControlEnabled(false);
            player.GetComponent<PlayerMotor>()?.SetMotorEnabled(false);
            // Raid result is terminal; debug infected must no longer damage an extracted player.
            foreach (var infected in FindObjectsByType<Outbreak.Infected.InfectedController>(FindObjectsSortMode.None))
                if (infected.Target == player) infected.StopForRaidEnd();
        }
        private void Cancel()
        {
            if (IsCountingDown) { cancelledUntil = Time.unscaledTime + 1.5f; LogTransition("cancelled"); }
            countdown?.Cancel(); countingSession = null;
        }
        private void OnTriggerEnter(Collider other)
        {
            var candidate = other.GetComponentInParent<PlayerHealth>();
            if (candidate == null || (player != null && player != candidate)) return;
            bool entering = occupants.Count == 0;
            player = candidate; occupants.Add(other);
            if (entering) LogTransition("entered");
        }
        private void OnTriggerStay(Collider other) { if (!occupants.Contains(other)) OnTriggerEnter(other); }
        private void OnTriggerExit(Collider other)
        { occupants.Remove(other); if (occupants.Count == 0) Cancel(); }
        private void OnDisable() { Cancel(); occupants.Clear(); player = null; }
    }
}
