using System;
using System.Collections.Generic;
using Dungeon.Logic.Core;
using UnityEngine;

namespace Dungeon.Logic.Services
{
    // System: manages underground fog-of-war and lazy obstacle generation.
    //
    // Underground cells (elevation < 0) are implicitly blocked unless dug out.
    // Digging a cell reveals it and its 8 neighbours. Revealed cells get obstacle
    // WorldObjects created for them (if not dug). The obstacle type equals the
    // cell's tile type. Fog-of-war (unrevealed rendering) is handled purely by
    // the visual layer — this service only tracks reveal/dig state.
    //
    // After digging, the dug cell's TileId becomes the type from one level below
    // (the new floor visible to the player).
    public class UndergroundService : ILogicService
    {
        private readonly int _emptyId;

        private GridService _grid;
        private WorldGenerationService _generation;
        private WorldObjectService _objects;
        private ObstacleService _obstacles;

        private readonly HashSet<Vector3Int> _revealedCells = new();
        private readonly HashSet<Vector3Int> _dugCells = new();

        // Maps revealed-but-not-dug cells to obstacle WorldObject IDs for cleanup.
        private readonly Dictionary<Vector3Int, int> _obstacleObjectIds = new();

        // Fired after cells are revealed — visual layer uses this to rebuild chunks.
        public event Action<IReadOnlyList<Vector3Int>> OnCellsRevealed;

        // Read-only access for serialisation (future).
        public IReadOnlyCollection<Vector3Int> RevealedCells => _revealedCells;
        public IReadOnlyCollection<Vector3Int> DugCells => _dugCells;

        // 8-direction neighbour offsets (same elevation).
        private static readonly Vector3Int[] NEIGHBOURS =
        {
            new Vector3Int( 1, 0,  0),
            new Vector3Int(-1, 0,  0),
            new Vector3Int( 0, 0,  1),
            new Vector3Int( 0, 0, -1),
            new Vector3Int( 1, 0,  1),
            new Vector3Int(-1, 0,  1),
            new Vector3Int( 1, 0, -1),
            new Vector3Int(-1, 0, -1),
        };

        public UndergroundService(int emptyId = 0)
        {
            _emptyId = emptyId;
        }

        public void Initialize(LogicWorld world)
        {
            _grid       = world.Get<GridService>();
            _generation = world.Get<WorldGenerationService>();
            _objects    = world.Get<WorldObjectService>();
            _obstacles  = world.Get<ObstacleService>();
        }

        public void Tick(float deltaTime) { }

        // ── Queries ────────────────────────────────────────────────────────────────────

        public static bool IsUnderground(int elevation) => elevation < 0;

        // Returns true if the cell is underground and has not been dug out.
        // Used by MovementService alongside ObstacleService.IsBlocked().
        public bool IsImplicitlyBlocked(Vector3Int cell)
        {
            if (cell.y >= 0) { return false; }
            return !_dugCells.Contains(cell);
        }

        public bool IsRevealed(Vector3Int cell) => _revealedCells.Contains(cell);

        public bool IsDug(Vector3Int cell) => _dugCells.Contains(cell);

        // Editor-driven reveal: the center cell is marked as dug (open space for
        // the painted floor) and its 8 neighbours are revealed with obstacle
        // auto-generation for undug cells.  Fires OnCellsRevealed so the visual
        // layer rebuilds affected chunks.
        public void EditorRevealArea(Vector3Int center)
        {
            List<Vector3Int> newlyRevealed = new List<Vector3Int>();

            // Center cell: dug + revealed. Remove any obstacle at this cell.
            _dugCells.Add(center);
            if (_revealedCells.Add(center))
            {
                newlyRevealed.Add(center);
            }
            if (_obstacleObjectIds.TryGetValue(center, out int obstacleId))
            {
                _obstacles.UnregisterObstacle(obstacleId);
                _objects.Remove(obstacleId);
                _obstacleObjectIds.Remove(center);
            }

            // Neighbours: reveal + create obstacles for undug cells.
            for (int i = 0; i < NEIGHBOURS.Length; i++)
            {
                RevealCell(center + NEIGHBOURS[i], newlyRevealed);
            }

            if (newlyRevealed.Count > 0)
            {
                OnCellsRevealed?.Invoke(newlyRevealed);
            }
        }

        // Updates the ObstacleTypeId of the obstacle WorldObject at the given cell.
        // Called when the floor one level above is painted, changing the material
        // this obstacle represents.
        public void UpdateObstacleType(Vector3Int cell, int newTypeId)
        {
            if (!_obstacleObjectIds.TryGetValue(cell, out int obstacleId)) { return; }
            if (!_objects.TryGet(obstacleId, out WorldObject obj)) { return; }
            if (!obj.TryGetFeature<Obstacle>(out Obstacle obstacle)) { return; }

            obstacle.ObstacleTypeId = newTypeId;
        }

        // ── Digging ────────────────────────────────────────────────────────────────────

        // Dig out a cell: remove its obstacle, reveal neighbours, update floor tile.
        public void DigCell(Vector3Int cell)
        {
            // 1. Mark as dug.
            _dugCells.Add(cell);

            // 2. Remove any existing obstacle WorldObject at this cell.
            if (_obstacleObjectIds.TryGetValue(cell, out int obstacleId))
            {
                _obstacles.UnregisterObstacle(obstacleId);
                _objects.Remove(obstacleId);
                _obstacleObjectIds.Remove(cell);
            }

            // 3. Update this cell's TileId to the floor type from one level below.
            //    The dug material is gone — the player now sees the surface of the layer below.
            int floorType = _generation.GetTileId(cell.x, cell.y - 1, cell.z);
            if (floorType == _emptyId)
            {
                floorType = _generation.GetTileId(cell.x, cell.y, cell.z);
            }
            Cell gridCell = _grid.Grid.GetCell(cell);
            if (gridCell != null)
            {
                gridCell.TileId = floorType;
            }

            // 4. Reveal this cell and all 8 neighbours.
            List<Vector3Int> newlyRevealed = new List<Vector3Int>();

            RevealCell(cell, newlyRevealed);
            for (int i = 0; i < NEIGHBOURS.Length; i++)
            {
                RevealCell(cell + NEIGHBOURS[i], newlyRevealed);
            }

            // 5. Include the dug cell in the rebuild set for visuals.
            if (!newlyRevealed.Contains(cell))
            {
                newlyRevealed.Add(cell);
            }

            // 6. Notify visual layer to rebuild affected chunks.
            if (newlyRevealed.Count > 0)
            {
                OnCellsRevealed?.Invoke(newlyRevealed);
            }
        }

        // ── Internal ───────────────────────────────────────────────────────────────────

        private void RevealCell(Vector3Int cell, List<Vector3Int> newlyRevealed)
        {
            // Only reveal underground cells.
            if (cell.y >= 0) { return; }

            // Already revealed — skip.
            if (!_revealedCells.Add(cell)) { return; }

            newlyRevealed.Add(cell);

            // Create obstacle WorldObject for revealed cells that have not been dug.
            if (!_dugCells.Contains(cell))
            {
                CreateObstacle(cell);
            }
        }

        private void CreateObstacle(Vector3Int cell)
        {
            // Don't create duplicates.
            if (_obstacleObjectIds.ContainsKey(cell)) { return; }

            // Obstacle type: prefer the floor tile from one level above when it has
            // been revealed or dug (meaning its TileId represents the floor = material
            // at this cell's elevation).  Fall back to the generated type.
            int obstacleTypeId;
            Vector3Int aboveCell = new Vector3Int(cell.x, cell.y + 1, cell.z);
            Cell above = _grid.Grid.GetCell(aboveCell);
            if (above != null && (_revealedCells.Contains(aboveCell) || _dugCells.Contains(aboveCell))
                && above.TileId != _emptyId)
            {
                obstacleTypeId = above.TileId;
            }
            else
            {
                obstacleTypeId = _generation.GetTileId(cell.x, cell.y, cell.z);
            }

            Vector3 worldPos = _grid.CellCenter(cell);
            var obj = new WorldObject("UndergroundWall", worldPos);
            obj.SetPosition(worldPos, _grid.CellSize, _grid.XZOffset, cell.y);
            obj.AddFeature(new Footprint(new List<Vector2Int> { Vector2Int.zero }));
            obj.AddFeature(new Obstacle(new List<Vector2Int> { Vector2Int.zero }, obstacleTypeId));
            _objects.Register(obj);
            _objects.OccupyCells(obj);
            _obstacles.RegisterObstacle(obj);

            _obstacleObjectIds[cell] = obj.Id;
        }

        public void Dispose()
        {
            _revealedCells.Clear();
            _dugCells.Clear();
            _obstacleObjectIds.Clear();
        }
    }
}
