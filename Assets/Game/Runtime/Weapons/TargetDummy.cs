using UnityEngine;

namespace Outbreak.Weapons
{
    public sealed class TargetDummy : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private float maximumHP = 150;
        [SerializeField] private bool autoReset = true;
        [SerializeField, Min(0.1f)] private float resetDelay = 2;
        public float RemainingHP { get; private set; }
        private float resetAt, flashUntil;
        private Renderer[] renderers;
        private MaterialPropertyBlock properties;
        private TextMesh label;
        private bool headshot;
        private void Awake()
        {
            RemainingHP = maximumHP;
            renderers = GetComponentsInChildren<Renderer>();
            properties = new MaterialPropertyBlock();
            label = GetComponentInChildren<TextMesh>();
            SetLabel($"{name}\nHP {RemainingHP:0}");
        }
        public void ApplyDamage(DamageInfo damage)
        {
            if (RemainingHP <= 0) return;
            RemainingHP = Mathf.Max(0, RemainingHP - damage.Damage);
            headshot = damage.IsHeadshot;
            flashUntil = Time.time + 0.12f;
            if (RemainingHP == 0) resetAt = Time.time + resetDelay;
            string message = $"{name}: {damage.Damage:0.#} damage | HP {RemainingHP:0.#}" + (headshot ? " | HEADSHOT" : "");
            SetLabel(message);
            Debug.Log("[Gunplay] " + message, this);
        }
        private void Update()
        {
            if (RemainingHP == 0 && autoReset && Time.time >= resetAt)
            { RemainingHP = maximumHP; SetLabel($"{name}\nHP {RemainingHP:0}"); }
            foreach (var renderer in renderers)
            {
                if (renderer.GetComponent<TextMesh>() != null) continue;
                properties.Clear();
                if (Time.time < flashUntil) properties.SetColor("_BaseColor", headshot ? Color.yellow : Color.white);
                else if (RemainingHP == 0) properties.SetColor("_BaseColor", Color.gray);
                renderer.SetPropertyBlock(properties);
            }
        }
        private void SetLabel(string text) { if (label != null) label.text = text; }
    }
}
