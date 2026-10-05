using System.Collections.Generic;
using Outbreak.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace Outbreak.Infected
{
    public sealed class InfectedSpawner : MonoBehaviour
    {
        [SerializeField] private InfectedController normalPrefab, runnerPrefab;
        [SerializeField] private PlayerHealth player;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField, Min(0)] private int normalCount = 4, runnerCount = 2;
        [SerializeField, Min(1)] private int maximum = 6;
        [SerializeField] private bool debugRespawn;
        [SerializeField, Min(1)] private float respawnInterval = 12;
        private readonly List<InfectedController> spawned = new List<InfectedController>();
        private float nextRespawn;
        private void Start() { SpawnMissing(); nextRespawn = Time.time + respawnInterval; }
        private void Update()
        {
            // Retire inactive corpses even without respawn, so repeated debug cycles stay bounded.
            for (int i = spawned.Count - 1; i >= 0; i--)
                if (spawned[i] == null || !spawned[i].gameObject.activeSelf)
                { if (spawned[i] != null) Destroy(spawned[i].gameObject); spawned.RemoveAt(i); }
            if (!debugRespawn || player == null || !player.IsAlive || Time.time < nextRespawn) return;
            nextRespawn = Time.time + respawnInterval; SpawnMissing();
        }
        private void SpawnMissing()
        {
            if (spawnPoints == null || spawnPoints.Length == 0 || player == null) return;
            int wanted = Mathf.Min(maximum, normalCount + runnerCount);
            for (int i = spawned.Count; i < wanted; i++)
            {
                var prefab = i < normalCount ? normalPrefab : runnerPrefab;
                var point = spawnPoints[i % spawnPoints.Length];
                if (prefab == null || point == null) continue;
                if (!NavMesh.SamplePosition(point.position, out var hit, 1.5f, NavMesh.AllAreas))
                { Debug.LogWarning("[Infected] Spawn point has no baked NavMesh: " + point.name, point); continue; }
                // Do not stack replacements on an occupied spawn.
                if (Physics.CheckSphere(hit.position + Vector3.up * 0.9f, 0.4f, ~0, QueryTriggerInteraction.Ignore)) continue;
                var infected = Instantiate(prefab, hit.position, point.rotation, transform);
                infected.Initialize(player); spawned.Add(infected);
            }
        }
    }
}
