using System.IO;
using Dungeon.Logic.Serialisation;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Reads and writes LevelData to/from JSON files.
    // Uses UnityEngine.JsonUtility, so lives in the Visuals assembly rather than Logic.
    public static class LevelDataSerializer
    {
        public static void Save(LevelData data, string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(filePath, json);
            Debug.Log($"[LevelDataSerializer] Saved '{data.Metadata.Name}' → {filePath}  ({data.Cells.Count} cells, {data.Objects.Count} objects)");
        }

        // Returns null and logs an error if the file does not exist or cannot be parsed.
        public static LevelData Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[LevelDataSerializer] File not found: {filePath}");
                return null;
            }

            string    json = File.ReadAllText(filePath);
            LevelData data = JsonUtility.FromJson<LevelData>(json);

            if (data == null)
            {
                Debug.LogError($"[LevelDataSerializer] Failed to parse JSON at: {filePath}");
                return null;
            }

            Debug.Log($"[LevelDataSerializer] Loaded '{data.Metadata.Name}' ← {filePath}  ({data.Cells.Count} cells, {data.Objects.Count} objects)");
            return data;
        }
    }
}
