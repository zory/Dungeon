using System;
using UnityEngine;

namespace Dungeon.Logic
{
    // Populates the Grid by asking a WorldDataSource for each cell's tile ID.
    // Swap the data source to change generation strategy (procedural, save file, etc.).
    // Runs at execution order -10 so the grid is ready before WorldRenderer's Start.
    [DefaultExecutionOrder(-10)]
    public class WorldGenerator : MonoBehaviour
    {
        [SerializeField] private GridManager    _gridManager;
        [SerializeField] private WorldDataSource _dataSource;

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
            {
                int tileId = _dataSource != null ? _dataSource.GetTileId(x, _elevation, z) : 0;
                grid.SetCell(new Vector3Int(x, _elevation, z), new Cell(tileId));
            }

            OnWorldGenerated?.Invoke();
        }
    }
}
