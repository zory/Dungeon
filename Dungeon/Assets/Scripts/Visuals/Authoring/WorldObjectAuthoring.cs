using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    // Authoring component for any WorldObject placed in the scene or spawned from a prefab.
    // Defines the object's name and which cells it occupies relative to its origin.
    // The origin cell is computed from the GameObject's transform position.
    //
    // Usage: add this to a prefab alongside a SpriteRenderer (or any visual).
    //        Set OccupiedCells to define the object's footprint.
    //        Example: a 1x1 barrel → [(0,0)], a 3x1 wall → [(0,0), (1,0), (2,0)].
    //        Vector2Int X = grid X offset, Y = grid Z offset.
    public class WorldObjectAuthoring : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string _objectName = "Object";

        [Header("Footprint — local cell offsets (X = grid X, Y = grid Z)")]
        [SerializeField] private List<Vector2Int> _occupiedCells = new() { Vector2Int.zero };

        public string ObjectName => _objectName;
        public List<Vector2Int> OccupiedCells => _occupiedCells;
    }
}
