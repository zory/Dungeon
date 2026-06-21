using System.Collections.Generic;
using Dungeon.Logic;
using Dungeon.Logic.Core;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Services;
using UnityEngine;

namespace Dungeon.Visuals.DebugTools
{
    // Inspector-driven debug tool for digging, creating obstacles, and changing cell types.
    // Enable/disable via the GameObject's active state in the hierarchy.
    //
    // Usage: fill in the fields in the Inspector, then tick the corresponding checkbox.
    // The action executes immediately and the checkbox resets.
    public class DebugCellTools : MonoBehaviour
    {
        [Header("Target Cell")]
        [SerializeField] private Vector3Int _cell;

        [Header("Dig (remove obstacle at cell)")]
        [SerializeField] private bool _dig;

        [Header("Create Obstacle")]
        [SerializeField] private int _obstacleTypeId = 4;
        [SerializeField] private bool _createObstacle;

        [Header("Change Cell Type (ground tile)")]
        [SerializeField] private int _newCellTypeId = 3;
        [SerializeField] private bool _changeCellType;

        private void OnValidate()
        {
            if (!Application.isPlaying) { return; }
            if (GameBootstrapper.Instance == null) { return; }

            if (_dig)
            {
                _dig = false;
                ExecuteDig();
            }

            if (_createObstacle)
            {
                _createObstacle = false;
                ExecuteCreateObstacle();
            }

            if (_changeCellType)
            {
                _changeCellType = false;
                ExecuteChangeCellType();
            }
        }

        private void ExecuteDig()
        {
            LogicWorld logic = GameBootstrapper.Instance.LogicWorld;
            UndergroundService underground = logic.Get<UndergroundService>();
            GridService grid = logic.Get<GridService>();

            // For underground cells, use the dig system which handles reveals + neighbour generation.
            if (_cell.y < 0)
            {
                // Ensure cell exists in the grid before digging.
                if (!grid.Grid.HasCell(_cell))
                {
                    WorldGenerationService generation = logic.Get<WorldGenerationService>();
                    int tileId = generation.GetTileId(_cell.x, _cell.y, _cell.z);
                    grid.Grid.SetCell(_cell, new Cell(tileId));
                }

                underground.DigCell(_cell);
                UnityEngine.Debug.Log($"[DebugCellTools] Dug cell {_cell}");
            }
            else
            {
                // Surface: just remove the obstacle WorldObject at this cell.
                WorldObjectService objects = logic.Get<WorldObjectService>();
                ObstacleService obstacles = logic.Get<ObstacleService>();

                int objectId = objects.GetObjectIdAtCell(_cell);
                if (objectId >= 0)
                {
                    obstacles.UnregisterObstacle(objectId);
                    objects.Remove(objectId);
                    UnityEngine.Debug.Log($"[DebugCellTools] Removed obstacle at {_cell} (objectId={objectId})");
                }
                else
                {
                    UnityEngine.Debug.Log($"[DebugCellTools] No obstacle at {_cell}");
                }
            }

            RebuildChunkAt(_cell);
        }

        private void ExecuteCreateObstacle()
        {
            LogicWorld logic = GameBootstrapper.Instance.LogicWorld;
            GridService grid = logic.Get<GridService>();
            WorldObjectService objects = logic.Get<WorldObjectService>();
            ObstacleService obstacles = logic.Get<ObstacleService>();

            // Don't place on top of an existing obstacle.
            if (obstacles.IsBlocked(_cell))
            {
                UnityEngine.Debug.LogWarning($"[DebugCellTools] Cell {_cell} is already blocked.");
                return;
            }

            int typeId = _obstacleTypeId;
            Vector3 worldPos = grid.CellCenter(_cell);
            var obj = new WorldObject($"DebugObstacle_{_obstacleTypeId}", worldPos);
            obj.SetPosition(worldPos, grid.CellSize, grid.XZOffset, _cell.y);
            obj.AddFeature(new Footprint(new List<Vector2Int> { Vector2Int.zero }));
            obj.AddFeature(new Obstacle(new List<Vector2Int> { Vector2Int.zero }, typeId));
            objects.Register(obj);
            objects.OccupyCells(obj);
            obstacles.RegisterObstacle(obj);

            UnityEngine.Debug.Log($"[DebugCellTools] Created obstacle (typeId={_obstacleTypeId}) at {_cell} (objectId={obj.Id})");
            RebuildChunkAt(_cell);
        }

        private void ExecuteChangeCellType()
        {
            LogicWorld logic = GameBootstrapper.Instance.LogicWorld;
            GridService grid = logic.Get<GridService>();

            grid.Grid.SetCell(_cell, new Cell(_newCellTypeId));

            // If underground, also update the obstacle type at this cell to match.
            if (_cell.y < 0)
            {
                WorldObjectService objects = logic.Get<WorldObjectService>();
                ObstacleService obstacles = logic.Get<ObstacleService>();

                int objectId = objects.GetObjectIdAtCell(_cell);
                if (objectId >= 0 && objects.TryGet(objectId, out WorldObject obj))
                {
                    if (obj.TryGetFeature<Obstacle>(out Obstacle obstacle))
                    {
                        // Remove old obstacle, update type, re-register.
                        obstacles.UnregisterObstacle(objectId);
                        obstacle.ObstacleTypeId = _newCellTypeId;
                        obstacles.RegisterObstacle(obj);
                    }
                }
            }

            UnityEngine.Debug.Log($"[DebugCellTools] Changed cell {_cell} type to {_newCellTypeId}");
            RebuildChunkAt(_cell);
        }

        private void RebuildChunkAt(Vector3Int cellCoord)
        {
            if (GameBootstrapper.Instance.VisualWorld == null) { return; }

            WorldRenderService render = GameBootstrapper.Instance.VisualWorld.Get<WorldRenderService>();
            Vector2Int chunk = GridService.CellToChunk(cellCoord.x, cellCoord.z);
            render.RebuildChunk(chunk);

            // Also rebuild cardinal neighbour chunks in case the cell is at a chunk edge.
            render.RebuildChunk(chunk + Vector2Int.up);
            render.RebuildChunk(chunk + Vector2Int.down);
            render.RebuildChunk(chunk + Vector2Int.left);
            render.RebuildChunk(chunk + Vector2Int.right);
        }
    }
}
