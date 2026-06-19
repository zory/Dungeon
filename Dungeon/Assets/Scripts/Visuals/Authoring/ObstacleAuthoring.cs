using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    // Authoring component that marks specific cells of a WorldObject as impassable.
    // Add this alongside WorldObjectAuthoring on the same GameObject.
    //
    // BlockedCells don't have to match OccupiedCells exactly — you can block only
    // some cells of a larger object. For example, a house (3x3 footprint) might
    // only block the wall cells while leaving the doorway open.
    //
    // Vector2Int X = grid X offset, Y = grid Z offset relative to origin.
    [RequireComponent(typeof(WorldObjectAuthoring))]
    public class ObstacleAuthoring : MonoBehaviour
    {
        [Header("Blocked cells — local offsets that are impassable")]
        [SerializeField] private List<Vector2Int> _blockedCells = new() { Vector2Int.zero };

        public List<Vector2Int> BlockedCells => _blockedCells;
    }
}
