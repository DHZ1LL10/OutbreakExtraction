using Outbreak.Player;
using Outbreak.Weapons;
using UnityEngine;

namespace Outbreak.Combat
{
    [RequireComponent(typeof(PlayerMotor), typeof(PlayerHealth)), DefaultExecutionOrder(180)]
    public sealed class PlayerNoiseEmitter : MonoBehaviour
    {
        [SerializeField, Min(0)] private float rifleRadius = 40, pistolRadius = 24;
        [SerializeField, Min(0.1f)] private float stepDistance = 1.5f;
        [SerializeField, Min(0)] private float movementNoiseMultiplier = 1;
        private PlayerMotor motor;
        private PlayerHealth health;
        private FirearmController firearm;
        private float distance, nextStep;
        public float LastNoiseRadius { get; private set; }
        private void Awake() { motor = GetComponent<PlayerMotor>(); health = GetComponent<PlayerHealth>(); firearm = GetComponent<FirearmController>(); }
        private void OnEnable() { if (firearm != null) firearm.OnShotFeedback += Shot; }
        private void OnDisable() { if (firearm != null) firearm.OnShotFeedback -= Shot; }
        private void Shot(WeaponShotFeedback shot)
        { if (health.IsAlive) Emit(shot.Position, NoiseRules.ShotRadius(shot.State.Definition.weaponClass, shot.NoiseMultiplier, rifleRadius, pistolRadius)); }
        private void Update()
        {
            if (!health.IsAlive || !motor.IsGrounded || motor.IsMantling || motor.Speed < 0.1f) { distance = 0; return; }
            distance += motor.Speed * Time.deltaTime;
            if (distance < stepDistance || Time.time < nextStep) return;
            distance = 0; nextStep = Time.time + 0.22f;
            Emit(transform.position, NoiseRules.MovementRadius(motor.IsSprinting, motor.IsWalkingSlow, motor.IsCrouching) * movementNoiseMultiplier);
        }
        private void Emit(Vector3 position, float radius)
        { LastNoiseRadius = radius; NoiseSystem.EmitNoise(position, radius, gameObject); }
    }
}
