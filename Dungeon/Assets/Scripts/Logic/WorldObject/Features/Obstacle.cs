using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Logic
{
    // Feature: marks specific local cells of this WorldObject as impassable.
    // BlockedLocalCells uses Vector2Int where X = grid X offset, Y = grid Z offset.
    // These cells don't have to match the Footprint exactly — an object can occupy
    // cells (0,0), (1,0), (2,0) but only block (0,0) and (2,0), leaving a gap.
    public class Obstacle
    {
        private readonly List<Vector2Int> _blockedLocalCells;

        public IReadOnlyList<Vector2Int> BlockedLocalCells => _blockedLocalCells;

        public Obstacle(List<Vector2Int> blockedLocalCells)
        {
            _blockedLocalCells = blockedLocalCells != null && blockedLocalCells.Count > 0
                ? new List<Vector2Int>(blockedLocalCells)
                : new List<Vector2Int> { Vector2Int.zero };
        }

        // Returns blocked world cell positions by offsetting each local cell from the origin.
        public List<Vector3Int> GetBlockedWorldCells(Vector3Int originCell)
        {
            var result = new List<Vector3Int>(_blockedLocalCells.Count);
            for (int i = 0; i < _blockedLocalCells.Count; i++)
            {
                Vector2Int local = _blockedLocalCells[i];
                result.Add(new Vector3Int(originCell.x + local.x, originCell.y, originCell.z + local.y));
            }
            return result;
        }

        // Non-allocating version — fills an existing list.
        public void GetBlockedWorldCells(Vector3Int originCell, List<Vector3Int> results)
        {
            results.Clear();
            for (int i = 0; i < _blockedLocalCells.Count; i++)
            {
                Vector2Int local = _blockedLocalCells[i];
                results.Add(new Vector3Int(originCell.x + local.x, originCell.y, originCell.z + local.y));
            }
        }
    }
}
