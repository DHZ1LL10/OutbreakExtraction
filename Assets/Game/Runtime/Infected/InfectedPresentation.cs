using Outbreak.Combat;
using Outbreak.Weapons;
using UnityEngine;

namespace Outbreak.Infected
{
    [RequireComponent(typeof(InfectedHealth))]
    public sealed class InfectedPresentation : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(0.1f)] private float collapseDuration = 0.7f;
        [SerializeField, Min(1)] private float corpseLifetime = 7;
        private InfectedHealth health;
        private InfectedController ai;
        private Renderer[] renderers;
        private MaterialPropertyBlock properties;
        private Vector3 basePosition, deathPosition, impulse;
        private Quaternion baseRotation, deathRotation;
        private float flashUntil, attackUntil, diedAt = -1;
        private void Awake()
        {
            health = GetComponent<InfectedHealth>(); ai = GetComponent<InfectedController>();
            if (visualRoot == null) { enabled = false; return; }
            basePosition = visualRoot.localPosition; baseRotation = visualRoot.localRotation;
            renderers = visualRoot.GetComponentsInChildren<Renderer>(); properties = new MaterialPropertyBlock();
        }
        private void OnEnable()
        { health.OnDamaged += Damaged; health.OnDied += Died; if (ai != null) ai.OnAttack += Attack; }
        private void OnDisable()
        { health.OnDamaged -= Damaged; health.OnDied -= Died; if (ai != null) ai.OnAttack -= Attack; }
        private void Attack() { attackUntil = Time.time + 0.2f; }
        private void Damaged(DamageInfo damage) { flashUntil = Time.time + 0.12f; }
        private void Died(DamageInfo damage)
        {
            if (diedAt >= 0) return;
            diedAt = Time.time; deathPosition = visualRoot.localPosition; deathRotation = visualRoot.localRotation;
            var away = damage.Source != null ? transform.position - damage.Source.transform.position : -damage.HitNormal;
            away.y = 0; impulse = transform.InverseTransformDirection(away.normalized) * 0.18f;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        }
        private void Update()
        {
            if (diedAt >= 0)
            {
                float t = Mathf.SmoothStep(0, 1, (Time.time - diedAt) / collapseDuration);
                visualRoot.localPosition = Vector3.Lerp(deathPosition, basePosition + impulse + Vector3.up * 0.22f, t);
                visualRoot.localRotation = Quaternion.Slerp(deathRotation, baseRotation * Quaternion.Euler(82, 0, 12), t);
                if (Time.time - diedAt > corpseLifetime) { gameObject.SetActive(false); return; }
            }
            else
            {
                float hit = Mathf.Clamp01((flashUntil - Time.time) / 0.12f);
                float attack = Mathf.Clamp01((attackUntil - Time.time) / 0.2f);
                visualRoot.localRotation = baseRotation * Quaternion.Euler(-hit * 5 + attack * 10, 0, hit * 3);
            }
            properties.Clear();
            if (!health.IsAlive) properties.SetColor("_BaseColor", new Color(0.2f, 0.22f, 0.2f));
            else if (Time.time < flashUntil) properties.SetColor("_BaseColor", new Color(0.9f, 0.8f, 0.5f));
            foreach (var renderer in renderers) renderer.SetPropertyBlock(properties);
        }
    }
}
