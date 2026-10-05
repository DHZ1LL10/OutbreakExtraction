using Outbreak.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Outbreak.Infected
{
    // Attach only to Infected_Test. No respawn/restart binding is added to the production player.
    public sealed class InfectedTestDebug : MonoBehaviour
    {
        [SerializeField] private PlayerHealth player;
        [SerializeField] private bool showLabels = true;
        private InfectedController[] infected = new InfectedController[0];
        private float nextScan;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (gameObject.scene.buildIndex >= 0) SceneManager.LoadScene(gameObject.scene.buildIndex);
                else Debug.LogWarning("Infected debug restart needs the generated scene enabled in Build Settings.", this);
            }
            if (Time.time < nextScan) return;
            nextScan = Time.time + 0.5f; infected = FindObjectsByType<InfectedController>(FindObjectsSortMode.None);
        }
        private void OnGUI()
        {
            if (player == null) return;
            if (!player.IsAlive)
            {
                int depth = GUI.depth; GUI.depth = -11000;
                GUI.Label(new Rect(Screen.width / 2f - 105, Screen.height / 2f + 35, 250, 30), "F9 - Restart Infected_Test");
                GUI.depth = depth;
            }
            GUI.Box(new Rect(12, 246, 345, 92), "Infected test — F9 restart");
            var noise = player.GetComponent<PlayerNoiseEmitter>();
            GUI.Label(new Rect(24, 272, 320, 60), $"HP: {player.CurrentHealth:0} / {player.MaxHealth:0}\nAlive: {player.IsAlive} | Last noise radius: {noise?.LastNoiseRadius ?? 0:0.0}m\nF1 optic | F2 muzzle | F3 mag | F4 grip");
            if (!showLabels || Camera.main == null) return;
            foreach (var actor in infected)
            {
                if (actor == null) continue;
                var point = Camera.main.WorldToScreenPoint(actor.transform.position + Vector3.up * 2.1f);
                if (point.z <= 0) continue;
                var health = actor.GetComponent<InfectedHealth>();
                GUI.Label(new Rect(point.x - 90, Screen.height - point.y - 20, 250, 65),
                    $"{actor.name}\n{actor.State} | HP {health.CurrentHealth:0} | target: {(actor.Target != null && actor.Target.IsAlive ? "Player" : "None")}\nDest {actor.Destination.x:0},{actor.Destination.z:0}");
            }
        }
    }
}
