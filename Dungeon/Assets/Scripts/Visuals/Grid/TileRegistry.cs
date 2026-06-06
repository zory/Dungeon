using System.Collections.Generic;
using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Maps TileType enum values to sprite-sheet indices (column-major linear index).
    // Create via Assets → Create → Dungeon → Tile Registry, then fill in the table.
    // TileType.None is never rendered regardless of any entry here.
    [CreateAssetMenu(menuName = "Dungeon/Tile Registry", fileName = "TileRegistry")]
    public class TileRegistry : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public TileType TileType;
            [Tooltip("Linear index into the sprite sheet (left-to-right, top-to-bottom from 0)")]
            public int SheetIndex;
        }

        [SerializeField] private Entry[] _entries;

        private Dictionary<int, int> _lookup;

        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            _lookup = new Dictionary<int, int>(_entries?.Length ?? 0);
            if (_entries == null) return;
            foreach (var e in _entries)
                _lookup[(int)e.TileType] = e.SheetIndex;
        }

        // Returns the sprite-sheet index for a given TileType int value.
        // Returns 0 (Darkness / first tile) if the type has no mapping.
        public int GetSheetIndex(int tileTypeValue)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(tileTypeValue, out int idx) ? idx : 0;
        }

        public bool IsNone(int tileTypeValue) => tileTypeValue == (int)TileType.None;
    }
}
