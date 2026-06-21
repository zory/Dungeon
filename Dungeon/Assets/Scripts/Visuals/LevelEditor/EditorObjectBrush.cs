using System.Collections.Generic;
using Dungeon.Logic;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Authoring;
using Dungeon.Visuals.Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals
{
    // Level editor tool: places and removes world objects on the grid.
    // Left-click = place selected object. Right-click = remove object at cell.
    // Reads GridService.HoveredCell (populated by GridInputService each frame).
    // Enable/disable via the GameObject's active state.
    public class EditorObjectBrush : MonoBehaviour
    {
        [Header("Object Selection")]
        [Tooltip("Database of placeable world objects.")]
        [SerializeField] private WorldObjectDatabase _database;

        [Tooltip("ID of the object to place (from WorldObjectDatabase).")]
        [SerializeField] private int _objectId = 1;

        // Tracks placed objects for clean removal.
        private readonly Dictionary<Vector3Int, PlacedEntry> _placed = new();

        private struct PlacedEntry
        {
            public int LogicObjectId;
            public GameObject Visual;
        }

        private GridService _grid;
        private WorldRenderService _worldRender;
        private WorldObjectService _objectService;
        private ObstacleService _obstacleService;

        private void Update()
        {
            if (!Application.isPlaying || GameBootstrapper.Instance == null) { return; }

            if (_grid == null)
            {
                _grid = GameBootstrapper.Instance.LogicWorld.Get<GridService>();
                _worldRender = GameBootstrapper.Instance.VisualWorld.Get<WorldRenderService>();
                _objectService = GameBootstrapper.Instance.LogicWorld.Get<WorldObjectService>();
                _obstacleService = GameBootstrapper.Instance.LogicWorld.Get<ObstacleService>();
            }

            Mouse mouse = Mouse.current;
            if (mouse == null) { return; }

            Vector3Int? hovered = _grid.HoveredCell;
            if (hovered == null) { return; }

            Vector3Int cell = new Vector3Int(hovered.Value.x, _grid.Elevation, hovered.Value.z);

            if (mouse.leftButton.wasPressedThisFrame)
            {
                PlaceObject(cell);
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                RemoveObject(cell);
            }
        }

        private void PlaceObject(Vector3Int cell)
        {
            if (_database == null)
            {
                Debug.LogWarning("[EditorObjectBrush] No WorldObjectDatabase assigned.");
                return;
            }

            if (!_database.TryGet(_objectId, out WorldObjectDatabase.Entry entry))
            {
                Debug.LogWarning($"[EditorObjectBrush] No entry with ID {_objectId} in database.");
                return;
            }

            if (entry.Prefab == null)
            {
                Debug.LogWarning($"[EditorObjectBrush] Entry '{entry.Name}' has no prefab.");
                return;
            }

            // Remove existing at this cell first.
            if (_placed.ContainsKey(cell))
            {
                RemoveObject(cell);
            }

            Vector3 worldPos = _grid.CellCenter(cell);
            GameObject instance = Instantiate(entry.Prefab, worldPos, Quaternion.identity);

            // Register in Logic.
            WorldObjectAuthoring authoring = instance.GetComponent<WorldObjectAuthoring>();
            string objectName = authoring != null ? authoring.ObjectName : entry.Name;
            List<Vector2Int> occupiedCells = authoring != null ? authoring.OccupiedCells : new List<Vector2Int> { Vector2Int.zero };

            var obj = new WorldObject(objectName, worldPos);
            obj.SetPosition(worldPos, _grid.CellSize, _grid.XZOffset, _grid.Elevation);
            obj.AddFeature(new Footprint(occupiedCells));
            _objectService.Register(obj);
            _objectService.OccupyCells(obj);

            // Register obstacle if the prefab has ObstacleAuthoring.
            ObstacleAuthoring obstacleAuthoring = instance.GetComponent<ObstacleAuthoring>();
            if (obstacleAuthoring != null)
            {
                obj.AddFeature(new Obstacle(obstacleAuthoring.BlockedCells));
                _obstacleService.RegisterObstacle(obj);
            }

            _placed[cell] = new PlacedEntry { LogicObjectId = obj.Id, Visual = instance };

            Debug.Log($"[EditorObjectBrush] Placed '{entry.Name}' at {cell}");
        }

        private void RemoveObject(Vector3Int cell)
        {
            // Try removing from our tracking first.
            if (_placed.TryGetValue(cell, out PlacedEntry placed))
            {
                if (_objectService.TryGet(placed.LogicObjectId, out WorldObject obj))
                {
                    if (obj.HasFeature<Obstacle>())
                    {
                        _obstacleService.UnregisterObstacle(obj.Id);
                    }
                    _objectService.Remove(obj.Id);
                }

                if (placed.Visual != null)
                {
                    Destroy(placed.Visual);
                }

                _placed.Remove(cell);
                Debug.Log($"[EditorObjectBrush] Removed object at {cell}");
                return;
            }

            // Fall back to removing whatever Logic object occupies this cell.
            WorldObject existing = _objectService.GetObjectAtCell(cell);
            if (existing != null)
            {
                if (existing.HasFeature<Obstacle>())
                {
                    _obstacleService.UnregisterObstacle(existing.Id);
                }
                _objectService.Remove(existing.Id);
                Debug.Log($"[EditorObjectBrush] Removed '{existing.Name}' at {cell}");
            }
        }

        private void OnDisable()
        {
            _placed.Clear();
        }
    }
}
