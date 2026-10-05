using System;
using System.IO;
using Outbreak.Core;
using Outbreak.Items;
using UnityEngine;

namespace Outbreak.Persistence
{
    public enum LoadResult { Loaded, Missing, Invalid }

    public sealed class SaveService
    {
        public string FilePath { get; }
        public SaveService(string directory = null, string fileName = "profile.json")
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
                throw new ArgumentException("Save file name must not contain directories.");
            FilePath = Path.Combine(directory ?? Application.persistentDataPath, fileName);
        }

        public bool TrySave(PlayerProfile profile, out string error)
        {
            error = null;
            try
            {
                string json = JsonUtility.ToJson(SaveMapper.Capture(profile), true);
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                string temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, json);
                // Replace only after the complete JSON has been written on the same volume.
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, null);
                else File.Move(temporary, FilePath);
                return true;
            }
            catch (Exception ex) when (IsExpected(ex)) { error = ex.Message; return false; }
        }

        public LoadResult Load(ItemCatalog catalog, out PlayerProfile profile, out string message)
        {
            profile = null;
            message = null;
            if (!File.Exists(FilePath)) { message = "No save file. New in-memory profile."; return LoadResult.Missing; }
            try
            {
                if (new FileInfo(FilePath).Length > 10 * 1024 * 1024) throw new ArgumentException("Save exceeds 10 MB limit.");
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                profile = SaveMapper.Restore(data, catalog);
                return LoadResult.Loaded;
            }
            catch (Exception ex) when (IsExpected(ex))
            {
                message = $"Save could not be loaded; original file preserved. {ex.Message}";
                return LoadResult.Invalid;
            }
        }
        private static bool IsExpected(Exception ex) => ex is IOException || ex is UnauthorizedAccessException ||
            ex is ArgumentException || ex is OverflowException || ex is NotSupportedException || ex is System.Security.SecurityException;
    }
}
