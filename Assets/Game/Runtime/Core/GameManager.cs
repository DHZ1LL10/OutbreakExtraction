using System;
using Outbreak.Items;
using Outbreak.Persistence;
using UnityEngine;

namespace Outbreak.Core
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private ItemCatalog catalog;
        [SerializeField] private string saveFileName = "profile.json";
        [SerializeField] private bool persistAcrossScenes;
        public static GameManager Persistent { get; private set; }
        public bool LoadSucceeded { get; private set; }
        public GameFlow Flow { get; private set; }
        public string SavePath { get; private set; }
        private void Awake()
        {
            if (Persistent != null && Persistent != this) { Destroy(gameObject); return; }
            try
            {
                var saves = new SaveService(fileName: saveFileName);
                SavePath = saves.FilePath;
                Flow = new GameFlow(catalog, saves);
                LoadSucceeded = Flow.Load();
                if (persistAcrossScenes) { Persistent = this; DontDestroyOnLoad(gameObject); }
                Debug.Log($"[OUTBREAK] {Flow.LastMessage} Path: {SavePath}", this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OUTBREAK] Initialization failed: {ex.Message}", this);
                enabled = false;
            }
        }
        private void OnDestroy() { if (Persistent == this) Persistent = null; }
    }
}
