using System;
using System.Collections.Generic;
using Dungeon.Logic.Core;
using UnityEngine;

namespace Dungeon.Logic.Services
{
    // System: tracks all impassable cells in the world.
    // WorldObjects with the Obstacle feature register their blocked cells here.
    // Walking characters query IsBlocked() before moving into a cell.
    public class ObstacleService : ILogicService
    {
        private readonly HashSet<Vector3Int> _blockedCells = new();
        private readonly Dictionary<int, List<Vector3Int>> _cellsByObjectId = new();

        // Fired when an obstacle WorldObject is registered (visual layer creates sprites).
        public event Action<WorldObject> OnObstacleRegistered;

        // Fired when an obstacle is removed by objectId (visual layer destroys sprites).
        public event Action<int> OnObstacleUnregistered;

        public void Initialize(LogicWorld world) { }

        public void Tick(float deltaTime) { }

        // Register all blocked cells for a WorldObject that has the Obstacle feature.
        public void RegisterObstacle(WorldObject obj)
        {
            if (!obj.TryGetFeature<Obstacle>(out Obstacle obstacle)) { return; }

            List<Vector3Int> worldCells = obstacle.GetBlockedWorldCells(obj.CellCoords);
            _cellsByObjectId[obj.Id] = worldCells;

            for (int i = 0; i < worldCells.Count; i++)
            {
                _blockedCells.Add(worldCells[i]);
            }

            OnObstacleRegistered?.Invoke(obj);
        }

        // Remove all blocked cells belonging to the given object.
        public void UnregisterObstacle(int objectId)
        {
            if (!_cellsByObjectId.TryGetValue(objectId, out List<Vector3Int> cells)) { return; }

            for (int i = 0; i < cells.Count; i++)
            {
                _blockedCells.Remove(cells[i]);
            }
            _cellsByObjectId.Remove(objectId);

            OnObstacleUnregistered?.Invoke(objectId);
        }

        // Returns true if the given world cell is blocked by any obstacle.
        public bool IsBlocked(Vector3Int worldCell) => _blockedCells.Contains(worldCell);

        // Returns all currently blocked cells (read-only snapshot for debug/visualization).
        public IReadOnlyCollection<Vector3Int> AllBlockedCells => _blockedCells;

        // Returns how many obstacle objects are registered.
        public int ObstacleCount => _cellsByObjectId.Count;

        public void Dispose()
        {
            _blockedCells.Clear();
            _cellsByObjectId.Clear();
        }
    }
}
