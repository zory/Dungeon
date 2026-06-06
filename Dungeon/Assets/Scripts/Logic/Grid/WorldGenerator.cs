using System;
using UnityEngine;

namespace Dungeon.Logic
{
    // Populates the Grid with an initial flat world of the given dimensions.
    // Runs at execution order -10 so the grid is filled before WorldRenderer's Start.
    // Subclass and override GetTileId to implement custom generation rules.
    [DefaultExecutionOrder(-10)]
    public class WorldGenerator : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;

        [Header("World Dimensions (in cells)")]
        [SerializeField] private int _widthInCells  = 128;
        [SerializeField] private int _lengthInCells = 128;

        [Header("Origin")]
        [SerializeField] private int _originX   = 0;
        [SerializeField] private int _originZ   = 0;
        // Must match the ElevationLayer of the GridRenderer showing this world layer
        [SerializeField] private int _elevation = 0;

        public event Action OnWorldGenerated;

        private void Start() => Generate();

        public void Generate()
        {
            var grid = _gridManager.Grid;
            grid.Clear();

            for (int x = _originX; x < _originX + _widthInCells;  x++)
            for (int z = _originZ; z < _originZ + _lengthInCells; z++)
                grid.SetCell(new Vector3Int(x, _elevation, z), new Cell(GetTileId(x, _elevation, z)));

            OnWorldGenerated?.Invoke();
        }

        // Override in subclasses to assign per-cell tile IDs during generation.
        protected virtual int GetTileId(int x, int y, int z) => 0;
    }
}
