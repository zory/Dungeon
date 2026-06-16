using System;
using Dungeon.Logic.Core;
using UnityEngine;

namespace Dungeon.Logic.Services
{
    [Serializable]
    public struct GridConfig
    {
        public float CellSize;
        public Vector2 XZOffset;

        public static GridConfig Default => new GridConfig { CellSize = 1f, XZOffset = Vector2.zero };
    }

    public class GridService : ILogicService
    {
        private readonly GridConfig _config;

        public CellGrid Grid { get; } = new CellGrid();
        public float CellSize => _config.CellSize;
        public Vector2 XZOffset => _config.XZOffset;
        public int Elevation { get; set; }

        // Hover / selection state
        public Vector3Int? HoveredCell { get; private set; }
        public Vector3Int? SelectedCell { get; private set; }
        public Vector3Int? PreviousSelectedCell { get; private set; }

        public event Action<Vector3Int?> OnHoverChanged;
        public event Action<Vector3Int?, Vector3Int?> OnSelectionChanged;

        public GridService(GridConfig config)
        {
            _config = config;
        }

        public void Initialize(LogicWorld world) { }

        public void Tick(float deltaTime) { }

        public void SetHovered(Vector3Int? coord)
        {
            if (HoveredCell == coord) return;
            HoveredCell = coord;
            OnHoverChanged?.Invoke(coord);
        }

        public void Select(Vector3Int? coord)
        {
            if (SelectedCell == coord) return;
            PreviousSelectedCell = SelectedCell;
            SelectedCell = coord;
            OnSelectionChanged?.Invoke(PreviousSelectedCell, SelectedCell);
        }

        public void Deselect() => Select(null);

        // World Y of the grid plane at the current elevation.
        public float WorldY => Elevation * _config.CellSize;

        // World-space center of a cell on the grid plane.
        public Vector3 CellCenter(Vector3Int cell)
        {
            float s = _config.CellSize;
            float x = (cell.x + 0.5f) * s + _config.XZOffset.x;
            float z = (cell.z + 0.5f) * s + _config.XZOffset.y;
            return new Vector3(x, WorldY, z);
        }

        // Converts a world position to the chunk coordinate it falls within.
        public Vector2Int WorldToChunk(Vector3 worldPos)
        {
            float chunkWorldSize = _config.CellSize * WorldConstants.ChunkSize;
            return new Vector2Int(
                Mathf.FloorToInt((worldPos.x - _config.XZOffset.x) / chunkWorldSize),
                Mathf.FloorToInt((worldPos.z - _config.XZOffset.y) / chunkWorldSize));
        }

        // Converts a cell coordinate to the chunk it belongs to.
        public static Vector2Int CellToChunk(int cellX, int cellZ) => new Vector2Int(
            Mathf.FloorToInt((float)cellX / WorldConstants.ChunkSize),
            Mathf.FloorToInt((float)cellZ / WorldConstants.ChunkSize));

        public void Dispose() { }
    }
}
