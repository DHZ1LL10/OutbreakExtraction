using Outbreak.Core;
using Outbreak.Maps;
using UnityEngine;

namespace Outbreak.Raids
{
    [DefaultExecutionOrder(-200), DisallowMultipleComponent]
    public sealed class RaidRuntimeContext : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private RaidMapDefinition map;
        [SerializeField, Min(1)] private int debugBackpackCapacity = 16;
        [SerializeField] private bool startRaidOnAwake = true;
        public GameFlow Flow => GameManager.Persistent != null ? GameManager.Persistent.Flow : gameManager != null ? gameManager.Flow : null;
        public RaidSession Session => Flow != null ? Flow.Session : null;
        public RaidMapDefinition Map => Session != null ? Session.Map : map;
        public float RaidStartedAt { get; private set; }
        public float ElapsedRaidTime => Mathf.Max(0, Time.unscaledTime - RaidStartedAt);

        private void Start()
        {
            if (GameManager.Persistent != null && Flow?.State == GameState.Raid)
            { RaidStartedAt = Time.unscaledTime; return; }
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (startRaidOnAwake && Flow != null && Flow.State == GameState.Hub)
            {
                if (!Flow.StartRaid(map, debugBackpackCapacity)) Debug.LogError("[Raid] Could not start debug raid: " + Flow.LastMessage, this);
                else RaidStartedAt = Time.unscaledTime;
            }
        }
    }
}
