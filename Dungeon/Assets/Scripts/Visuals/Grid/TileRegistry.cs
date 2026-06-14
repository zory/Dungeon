using System.Collections.Generic;
using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Maps each TileType to a render color and a priority for dual-grid autotile layering.
    // Higher priority types are drawn on top at transitions between two different tile types.
    // TileType.None is never rendered; it acts as transparent / empty space.
    [CreateAssetMenu(menuName = "Dungeon/Tile Color Registry", fileName = "TileColorRegistry")]
    public class TileColorRegistry : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public TileType TileType;
            public Color    Color;
            [Tooltip("Higher value = drawn on top when two types share a dual-grid corner.")]
            public int      Priority;
        }

        [SerializeField] private List<Entry> _entries   = new();
        [SerializeField] private Color       _noneColor = new Color(0, 0, 0, 0);

        private Dictionary<int, Entry> _lookup;

        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            _lookup = new Dictionary<int, Entry>(_entries?.Count ?? 0);
            if (_entries == null) return;
            foreach (var e in _entries)
                _lookup[(int)e.TileType] = e;
        }

        public bool  IsNone    (int tileTypeValue) => tileTypeValue == (int)TileType.None;
        public Color NoneColor => _noneColor;

        public Color GetColor(int tileTypeValue)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(tileTypeValue, out var e) ? e.Color : _noneColor;
        }

        public int GetPriority(int tileTypeValue)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(tileTypeValue, out var e) ? e.Priority : -1;
        }
    }
}
