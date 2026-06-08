using System.Collections.Generic;
using Dungeon.Logic.Serialisation;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Holds current editor tool settings and tracks every placed object so they can be
    // erased, re-placed, and serialised.  Cell data is NOT mirrored here — the
    // authoritative source for cells is GridManager.Grid.
    public class EditorState : MonoBehaviour
    {
        public enum Tool { Paint, PlaceObject, Erase }

        [Header("Active Tool")]
        public Tool CurrentTool = Tool.Paint;

        [Header("Paint")]
        public int SelectedTileTypeId = (int)Dungeon.Logic.TileType.Grass;

        [Header("Place Object")]
        public string SelectedObjectTypeId = "";

        [Header("Elevation  (PageUp / PageDown to switch)")]
        public int ActiveElevation = 0;

        [Header("Save / Load")]
        public string LevelName = "unnamed";

        // coord → (typeId, scene GameObject) for every placed object
        private readonly Dictionary<Vector3Int, (string TypeId, GameObject Go)> _placedObjects = new();

        // ── Object tracking ────────────────────────────────────────────────────────────

        // Called by EditorToolController when a new object is instantiated.
        public void PlaceObject(Vector3Int coord, string typeId, GameObject go)
        {
            // If something is already here, destroy it first
            if (_placedObjects.TryGetValue(coord, out var existing) && existing.Go != null)
                Destroy(existing.Go);

            _placedObjects[coord] = (typeId, go);
        }

        // Returns true and outputs the GO if an object existed at coord (and removes it from tracking).
        // The caller is responsible for actually destroying the returned GO.
        public bool TryEraseObject(Vector3Int coord, out GameObject go)
        {
            if (_placedObjects.TryGetValue(coord, out var entry))
            {
                go = entry.Go;
                _placedObjects.Remove(coord);
                return true;
            }
            go = null;
            return false;
        }

        // Destroys all tracked GOs and clears tracking — used when loading a new level.
        public void ClearAllObjects()
        {
            foreach (var (_, entry) in _placedObjects)
            {
                if (entry.Go != null) Destroy(entry.Go);
            }
            _placedObjects.Clear();
        }

        // Called after LevelLoader spawns a prefab so subsequent saves / erases work correctly.
        public void RegisterLoadedObject(Vector3Int coord, string typeId, GameObject go)
        {
            _placedObjects[coord] = (typeId, go);
        }

        // ── Serialisation helpers ──────────────────────────────────────────────────────

        // Builds ObjectData entries from the current placement tracking.
        // Cells are NOT included — EditorToolController reads those directly from GridManager.Grid.
        public List<ObjectData> BuildObjectDataList()
        {
            var list = new List<ObjectData>(_placedObjects.Count);
            foreach (var (coord, entry) in _placedObjects)
            {
                list.Add(new ObjectData
                {
                    TypeId = entry.TypeId,
                    X      = coord.x,
                    Y      = coord.y,
                    Z      = coord.z,
                });
            }
            return list;
        }
    }
}
