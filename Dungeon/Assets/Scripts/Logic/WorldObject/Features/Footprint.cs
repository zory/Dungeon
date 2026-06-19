using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Logic
{
    // Feature: defines which cells this WorldObject occupies, relative to its origin cell.
    // LocalCells uses Vector2Int where X = grid X offset, Y = grid Z offset.
    // A single-cell object has one entry: (0, 0).
    // A 2-wide object might have: (0, 0), (1, 0).
    public class Footprint
    {
        private readonly List<Vector2Int> _localCells;

        public IReadOnlyList<Vector2Int> LocalCells => _localCells;

        public Footprint(List<Vector2Int> localCells)
        {
            _localCells = localCells != null && localCells.Count > 0
                ? new List<Vector2Int>(localCells)
                : new List<Vector2Int> { Vector2Int.zero };
        }

        // Returns world cell positions by offsetting each local cell from the origin.
        // Origin's Y (elevation) is applied to all cells.
        public List<Vector3Int> GetWorldCells(Vector3Int originCell)
        {
            var result = new List<Vector3Int>(_localCells.Count);
            for (int i = 0; i < _localCells.Count; i++)
            {
                Vector2Int local = _localCells[i];
                result.Add(new Vector3Int(originCell.x + local.x, originCell.y, originCell.z + local.y));
            }
            return result;
        }

        // Non-allocating version — fills an existing list.
        public void GetWorldCells(Vector3Int originCell, List<Vector3Int> results)
        {
            results.Clear();
            for (int i = 0; i < _localCells.Count; i++)
            {
                Vector2Int local = _localCells[i];
                results.Add(new Vector3Int(originCell.x + local.x, originCell.y, originCell.z + local.y));
            }
        }
    }
}
