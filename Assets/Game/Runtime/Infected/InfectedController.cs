using System;
using Outbreak.Combat;
using Outbreak.Weapons;
using UnityEngine;
using UnityEngine.AI;

namespace Outbreak.Infected
{
    [RequireComponent(typeof(NavMeshAgent), typeof(InfectedHealth))]
    public sealed class InfectedController : MonoBehaviour
    {
        public InfectedSettings settings = new InfectedSettings();
        [SerializeField] private Transform eyes;
        [SerializeField] private PlayerHealth target;
        [SerializeField] private bool showDebug = true;
        private NavMeshAgent agent;
        private InfectedHealth health;
        private InfectedBrain brain;
        private IDisposable noiseSubscription;
        private Vector3 goal, home, lastKnown;
        private float nextSense, nextDestination;
        private bool visible, goalValid;
        public InfectedState State => brain != null ? brain.State : InfectedState.Idle;
        public PlayerHealth Target => target;
        public Vector3 Destination => goal;
        public event Action OnAttack;
        public void Initialize(PlayerHealth player) { target = player; }
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>(); health = GetComponent<InfectedHealth>();
            brain = new InfectedBrain(settings, Time.timeAsDouble);
            brain.OnStateChanged += StateChanged; brain.OnStrike += Strike;
            home = goal = transform.position;
        }
        private void OnEnable()
        {
            health.OnDamaged += Damaged; health.OnDied += Died;
            noiseSubscription = NoiseSystem.Bus.Subscribe(() => transform.position, settings.hearingRange, Heard);
        }
        private void OnDisable()
        {
            health.OnDamaged -= Damaged; health.OnDied -= Died; noiseSubscription?.Dispose();
        }
        private void Start()
        {
            if (target == null) target = FindFirstObjectByType<PlayerHealth>();
            if (!agent.isOnNavMesh) { Debug.LogError("[Infected] Spawn is outside the baked NavMesh.", this); enabled = false; return; }
            agent.stoppingDistance = settings.attackRange * 0.7f;
            agent.isStopped = true;
            brain.WaitUntil(Time.timeAsDouble + UnityEngine.Random.Range(settings.idleMinimum, settings.idleMaximum));
        }
        private void Update()
        {
            if (!health.IsAlive || !agent.enabled || !agent.isOnNavMesh) return;
            bool alive = target != null && target.IsAlive;
            if (Time.time >= nextSense)
            {
                nextSense = Time.time + Mathf.Max(0.05f, settings.perceptionInterval);
                visible = alive && CanSeeTarget();
                if (visible) lastKnown = target.transform.position;
            }
            if (State == InfectedState.Chase && visible) { goal = lastKnown; goalValid = true; }
            bool arrived = !goalValid || (!agent.pathPending && Vector3.Distance(transform.position, goal) <= agent.stoppingDistance + 0.15f);
            float distance = alive ? Vector3.Distance(transform.position, target.transform.position) : float.PositiveInfinity;
            brain.Tick(Time.timeAsDouble, new InfectedObservation(visible, alive, distance, arrived));
            bool moving = State == InfectedState.Wander || (State == InfectedState.Investigate && !arrived) || State == InfectedState.Chase;
            agent.isStopped = !moving;
            agent.updateRotation = moving;
            agent.speed = State == InfectedState.Chase ? settings.chaseSpeed : settings.wanderSpeed;
            if (moving && goalValid && Time.time >= nextDestination)
            {
                nextDestination = Time.time + Mathf.Max(0.1f, settings.destinationInterval);
                // Gunshot origins are above the floor. Keep the projected point for arrival checks too.
                if (NavMesh.SamplePosition(goal, out var hit, 3, agent.areaMask))
                { goal = hit.position; agent.SetDestination(goal); }
                else { goalValid = false; agent.ResetPath(); }
            }
            if (State == InfectedState.Attack && alive)
            {
                var direction = target.transform.position - transform.position; direction.y = 0;
                if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 240 * Time.deltaTime);
            }
            else if (State == InfectedState.Investigate && arrived) transform.Rotate(0, 40 * Time.deltaTime, 0);
        }
        private bool CanSeeTarget()
        {
            if (target == null || !target.IsAlive) return false;
            var origin = eyes != null ? eyes.position : transform.position + Vector3.up * 1.5f;
            var to = target.TargetPoint - origin;
            var horizontal = new Vector3(to.x, 0, to.z);
            if (to.magnitude > settings.visionRange || Vector3.Angle(transform.forward, horizontal) > settings.visionAngle * 0.5f) return false;
            return ClearLine(origin, target.TargetPoint);
        }
        private bool ClearLine(Vector3 origin, Vector3 end)
        {
            var delta = end - origin;
            foreach (var hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && (target == null || !hit.transform.IsChildOf(target.transform))) return false;
            return true;
        }
        private void Heard(NoiseEvent noise)
        {
            if (!health.IsAlive || noise.Source == gameObject || (target != null && !target.IsAlive)) return;
            float importance = noise.Radius - Vector3.Distance(transform.position, noise.Position);
            if (!brain.Hear(importance, Time.timeAsDouble)) return;
            goal = noise.Position; goalValid = true; nextDestination = 0;
        }
        private void StateChanged(InfectedState state)
        {
            nextDestination = 0;
            if (state == InfectedState.Chase) { goal = lastKnown; goalValid = true; }
            if (state == InfectedState.Idle) brain.WaitUntil(Time.timeAsDouble + UnityEngine.Random.Range(settings.idleMinimum, settings.idleMaximum));
            if (state == InfectedState.Wander)
            {
                goalValid = false;
                for (int i = 0; i < 6; i++)
                {
                    var disk = UnityEngine.Random.insideUnitCircle * settings.wanderRadius;
                    if (!NavMesh.SamplePosition(home + new Vector3(disk.x, 0, disk.y), out var hit, 1, agent.areaMask)) continue;
                    var path = new NavMeshPath();
                    if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                    goal = hit.position; goalValid = true; break;
                }
            }
        }
        private void Strike()
        {
            // Windup completion rechecks current position/LOS, independent of cached vision.
            if (!health.IsAlive || target == null || !target.IsAlive || State != InfectedState.Attack) return;
            if (Vector3.Distance(transform.position, target.transform.position) > settings.attackRange) return;
            var start = eyes != null ? eyes.position : transform.position + Vector3.up;
            if (!ClearLine(start, target.TargetPoint)) return;
            OnAttack?.Invoke();
            target.ApplyDamage(new DamageInfo(settings.attackDamage, target.TargetPoint, (target.transform.position - transform.position).normalized, gameObject));
        }
        private void Damaged(DamageInfo damage)
        {
            if (!health.IsAlive) return;
            if (damage.Source != null) { goal = damage.Source.transform.position; goalValid = true; brain.Hear(float.MaxValue, Time.timeAsDouble); }
            brain.Stagger(damage.Damage, Time.timeAsDouble);
        }
        private void Died(DamageInfo damage)
        {
            brain.Die(Time.timeAsDouble); noiseSubscription?.Dispose();
            if (agent.enabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
            agent.enabled = false;
        }
        private void OnDrawGizmosSelected()
        {
            if (!showDebug) return;
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, settings.hearingRange);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, settings.attackRange);
            Vector3 origin = eyes != null ? eyes.position : transform.position + Vector3.up * 1.5f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(origin, Quaternion.AngleAxis(-settings.visionAngle / 2, Vector3.up) * transform.forward * settings.visionRange);
            Gizmos.DrawRay(origin, Quaternion.AngleAxis(settings.visionAngle / 2, Vector3.up) * transform.forward * settings.visionRange);
            if (Application.isPlaying) { Gizmos.color = Color.green; Gizmos.DrawLine(transform.position, goal); Gizmos.DrawWireSphere(goal, 0.2f); }
        }
    }
}
