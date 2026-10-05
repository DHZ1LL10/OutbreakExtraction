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
        public GameFlow Flow { get; private set; }
        public string SavePath { get; private set; }
        private void Awake()
        {
            try
            {
                var saves = new SaveService(fileName: saveFileName);
                SavePath = saves.FilePath;
                Flow = new GameFlow(catalog, saves);
                Flow.Load();
                Debug.Log($"[OUTBREAK] {Flow.LastMessage} Path: {SavePath}", this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OUTBREAK] Initialization failed: {ex.Message}", this);
                enabled = false;
            }
        }
    }
}
