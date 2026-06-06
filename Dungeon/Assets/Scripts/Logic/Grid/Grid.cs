using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Logic
{
    // Infinite 3D grid. XZ = horizontal plane, Y = elevation.
    // Cells are stored sparsely — only occupied coordinates consume memory.
    public class Grid
    {
        private readonly Dictionary<Vector3Int, Cell> _cells = new();

        public void SetCell(Vector3Int coord, Cell cell) => _cells[coord] = cell;

        public Cell GetCell(Vector3Int coord) => _cells.TryGetValue(coord, out var cell) ? cell : null;

        public bool HasCell(Vector3Int coord) => _cells.ContainsKey(coord);

        public void RemoveCell(Vector3Int coord) => _cells.Remove(coord);

        public IEnumerable<Vector3Int> GetAllCoordinates() => _cells.Keys;

        public int Count => _cells.Count;
    }
}
