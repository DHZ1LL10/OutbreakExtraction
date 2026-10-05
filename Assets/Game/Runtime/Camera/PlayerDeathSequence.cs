using System;
using Outbreak.Combat;
using Outbreak.Core;
using Outbreak.Player;
using Outbreak.Weapons;
using UnityEngine;

namespace Outbreak.Cameras
{
    public enum PlayerLifeAudioCue { Hurt, FinalImpact, TinnitusMuffle, DeathComplete }
    [DefaultExecutionOrder(300), RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerDeathSequence : MonoBehaviour
    {
        [SerializeField, Range(2, 4)] private float duration = 3.2f;
        [SerializeField] private BodycamController bodycam;
        [SerializeField] private GameManager gameManager;
        private PlayerHealth health;
        private DeathLifecycle lifecycle;
        private RaidDeathCompletion raidCompletion;
        private Vector3 startPosition, endPosition;
        private Quaternion startRotation, endRotation;
        private float damageFlashUntil;
        public bool IsDying => lifecycle != null && lifecycle.Started && !lifecycle.Completed;
        public bool IsComplete => lifecycle != null && lifecycle.Completed;
        public event Action OnDeathStarted;
        public event Action OnDeathCompleted;
        public event Action<PlayerLifeAudioCue> OnAudioCue;
        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            if (bodycam == null) bodycam = GetComponentInChildren<BodycamController>();
            lifecycle = new DeathLifecycle(duration);
            lifecycle.OnCompleted += Complete;
        }
        private void OnEnable() { health.OnDamaged += Damaged; health.OnDied += Begin; }
        private void OnDisable() { health.OnDamaged -= Damaged; health.OnDied -= Begin; }
        private void Damaged(DamageInfo damage)
        {
            if (!health.IsAlive) return;
            bodycam?.AddWeaponKick(Mathf.Clamp(damage.Damage * 0.025f, 0.15f, 0.7f));
            damageFlashUntil = Time.unscaledTime + 0.12f; OnAudioCue?.Invoke(PlayerLifeAudioCue.Hurt);
        }
        private void Begin(DamageInfo damage)
        {
            if (!lifecycle.TryBegin()) return;
            GetComponent<PlayerInput>()?.SetControlEnabled(false);
            GetComponent<PlayerMotor>()?.SetMotorEnabled(false);
            var look = GetComponent<PlayerLook>(); if (look != null) look.enabled = false;
            var firearm = GetComponent<FirearmController>(); if (firearm != null) firearm.enabled = false;
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            raidCompletion = new RaidDeathCompletion(gameManager != null ? gameManager.Flow : null);
            if (bodycam != null)
            {
                bodycam.EnterDeathState();
                startPosition = bodycam.transform.position; startRotation = bodycam.transform.rotation;
                // Fall vertically at the current camera footprint: no lateral teleport through cover.
                float best = float.PositiveInfinity; endPosition = startPosition;
                foreach (var hit in Physics.RaycastAll(startPosition, Vector3.down, 4, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform.IsChildOf(transform) || hit.distance >= best || hit.normal.y < 0.5f) continue;
                    best = hit.distance; endPosition = hit.point + Vector3.up * 0.18f;
                }
                endRotation = Quaternion.Euler(18, startRotation.eulerAngles.y, 76);
            }
            OnDeathStarted?.Invoke(); OnAudioCue?.Invoke(PlayerLifeAudioCue.FinalImpact); OnAudioCue?.Invoke(PlayerLifeAudioCue.TinnitusMuffle);
        }
        private void LateUpdate()
        {
            if (!lifecycle.Started) return;
            lifecycle.Advance(Time.unscaledDeltaTime);
            float fall = Mathf.SmoothStep(0, 1, Mathf.Clamp01(lifecycle.Progress / 0.65f));
            if (bodycam != null)
            {
                Vector3 desired = Vector3.Lerp(startPosition, endPosition, fall);
                // Sweep each segment so a moving obstacle cannot be crossed during the fall.
                Vector3 from = bodycam.transform.position, delta = desired - from;
                float allowed = delta.magnitude;
                if (allowed > 0.0001f)
                    foreach (var hit in Physics.SphereCastAll(from, 0.07f, delta.normalized, allowed, ~0, QueryTriggerInteraction.Ignore))
                        if (!hit.transform.IsChildOf(transform)) allowed = Mathf.Min(allowed, Mathf.Max(0, hit.distance - 0.01f));
                var rotation = Quaternion.Slerp(startRotation, endRotation, fall) * Quaternion.Euler(Mathf.Sin(lifecycle.Progress * 35) * (1 - fall) * 4, 0, 0);
                bodycam.SetDeathPose(from + delta.normalized * allowed, rotation);
            }
        }
        private void Complete()
        { raidCompletion?.Complete(); OnAudioCue?.Invoke(PlayerLifeAudioCue.DeathComplete); OnDeathCompleted?.Invoke(); }
        private void OnGUI()
        {
            float alpha = lifecycle.Started ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.5f, 1, lifecycle.Progress)) :
                Mathf.Clamp01((damageFlashUntil - Time.unscaledTime) / 0.12f) * 0.06f;
            if (alpha <= 0) return;
            int depth = GUI.depth; Color color = GUI.color; GUI.depth = -10000;
            GUI.color = lifecycle.Started ? new Color(0, 0, 0, alpha) : new Color(0.8f, 0.8f, 0.7f, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            if (lifecycle.Completed) { GUI.color = Color.white; GUI.Label(new Rect(Screen.width / 2f - 50, Screen.height / 2f, 180, 30), "DEAD"); }
            GUI.color = color; GUI.depth = depth;
        }
    }
}
