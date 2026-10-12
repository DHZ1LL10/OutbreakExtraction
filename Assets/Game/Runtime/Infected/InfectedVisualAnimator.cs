using Outbreak.Combat;
using Outbreak.Weapons;
using UnityEngine;
using UnityEngine.AI;

namespace Outbreak.Infected
{
    // Presentation adapter only. AI/health remain authoritative for movement and strikes.
    [DefaultExecutionOrder(50), RequireComponent(typeof(InfectedController), typeof(InfectedHealth))]
    public sealed class InfectedVisualAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform headHitbox;
        [SerializeField] private bool runner;
        private InfectedController controller;
        private InfectedHealth health;
        private NavMeshAgent agent;
        private Transform head;
        private string pose;
        private float attackUntil, damageUntil;

        private void Awake()
        {
            controller = GetComponent<InfectedController>();
            health = GetComponent<InfectedHealth>(); agent = GetComponent<NavMeshAgent>();
            if (animator == null) { enabled = false; return; }
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (animator.isHuman) head = animator.GetBoneTransform(HumanBodyBones.Head);
        }
        private void OnEnable()
        { health.OnDamaged += Damaged; health.OnDied += Died; controller.OnAttack += Attack; }
        private void OnDisable()
        { health.OnDamaged -= Damaged; health.OnDied -= Died; controller.OnAttack -= Attack; }
        private void Attack()
        {
            if (!health.IsAlive) return;
            attackUntil = Time.time + 0.65f; Play("Attack", true);
        }
        private void Damaged(DamageInfo info)
        { if (health.IsAlive) { damageUntil = Time.time + 0.3f; Play("Damage", true); } }
        private void Died(DamageInfo info) { Play("Death", true); }
        private void Play(string next, bool restart = false)
        {
            if (animator == null || (pose == next && !restart)) return;
            pose = next; animator.speed = 1;
            animator.CrossFadeInFixedTime(next, next == "Death" ? 0.07f : 0.12f);
        }
        private void Update()
        {
            if (animator == null) return;
            if (!health.IsAlive) { Play("Death"); return; }
            if (Time.time < damageUntil || controller.State == InfectedState.Stagger) { Play("Damage"); return; }
            if (Time.time < attackUntil) { Play("Attack"); return; }
            if (controller.State == InfectedState.Attack && pose != "Attack")
            { attackUntil = Time.time + 0.65f; Play("Attack"); return; }
            float speed = agent.enabled ? agent.velocity.magnitude : 0;
            Play(speed > 0.08f ? "Walk" : "Idle");
            if (pose == "Walk") animator.speed = Mathf.Clamp(speed / 0.85f, 0.65f, runner ? 3.5f : 2.6f);
        }
        private void LateUpdate()
        {
            // Keep the existing Head DamageHitbox on the animated head; never add mesh damage colliders.
            if (health.IsAlive && head != null && headHitbox != null)
                headHitbox.position = head.position + transform.up * 0.04f;
        }
    }
}
