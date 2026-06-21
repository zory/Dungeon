using System.Collections.Generic;
using Dungeon.Logic;
using Dungeon.Logic.Serialisation;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Authoring;
using Dungeon.Visuals.Services;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Scene MonoBehaviour providing inspector-driven level editing tools.
    // Set coordinates and parameters in the inspector, then press buttons
    // (via the custom editor) to paint/erase terrain or place/remove world objects.
    // Supports full save/load cycle — persists terrain + world objects to JSON.
    // Only works in Play mode — requires GameBootstrapper to be running.
    public class LevelEditorTools : MonoBehaviour
    {
        [Header("Databases")]
        [SerializeField] private WorldObjectDatabase _worldObjectDatabase;

        [Header("Save / Load")]
        [SerializeField] private string _savePath = "Assets/Levels/level.json";
        [SerializeField] private string _levelName = "unnamed";
        [SerializeField] private bool _autoLoadOnStart = true;

        [Header("Terrain Tool")]
        [SerializeField] private Vector3Int _terrainCell;
        [SerializeField] private int _terrainTypeId = 3;

        [Header("World Object Tool")]
        [SerializeField] private Vector3Int _objectCell;
        [SerializeField] private int _objectDatabaseId = 1;

        // Tracks objects placed by this tool for clean removal.
        private readonly Dictionary<Vector3Int, PlacedObject> _placedObjects = new();

        private struct PlacedObject
        {
            public int LogicObjectId;
            public GameObject Visual;
            public string TypeId;
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Start()
        {
            if (_autoLoadOnStart && Application.isPlaying && GameBootstrapper.Instance != null)
            {
                if (System.IO.File.Exists(_savePath))
                {
                    LoadLevel();
                }
            }
        }

        // ── Save / Load ──────────────────────────────────────────────────────

        public void SaveLevel()
        {
            if (!EnsurePlaying()) { return; }
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            GridService grid = bootstrapper.LogicWorld.Get<GridService>();

            LevelData data = new LevelData();
            data.Metadata.Name = _levelName;

            // Save all grid cells.
            foreach (Vector3Int coord in grid.Grid.GetAllCoordinates())
            {
                Cell cell = grid.Grid.GetCell(coord);
                if (cell == null) { continue; }
                data.Cells.Add(new CellData
                {
                    X = coord.x,
                    Y = coord.y,
                    Z = coord.z,
                    TileTypeId = cell.TileId,
                });
            }

            // Save placed world objects.
            foreach (KeyValuePair<Vector3Int, PlacedObject> kvp in _placedObjects)
            {
                Vector3Int coord = kvp.Key;
                PlacedObject placed = kvp.Value;
                if (string.IsNullOrEmpty(placed.TypeId)) { continue; }

                data.Objects.Add(new ObjectData
                {
                    TypeId = placed.TypeId,
                    X = coord.x,
                    Y = coord.y,
                    Z = coord.z,
                });
            }

            LevelDataSerializer.Save(data, _savePath);
        }

        public void LoadLevel()
        {
            if (!EnsurePlaying()) { return; }

            LevelData data = LevelDataSerializer.Load(_savePath);
            if (data == null) { return; }

            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            GridService grid = bootstrapper.LogicWorld.Get<GridService>();
            ChunkLoadingService chunkLoader = bootstrapper.LogicWorld.Get<ChunkLoadingService>();

            // Clear existing state.
            ClearAllPlacedObjects();
            grid.Grid.Clear();
            chunkLoader.Reset();

            // Restore grid cells.
            foreach (CellData cellData in data.Cells)
            {
                Vector3Int coord = new Vector3Int(cellData.X, cellData.Y, cellData.Z);
                grid.Grid.SetCell(coord, new Cell(cellData.TileTypeId));

                // Mark chunk as loaded so procedural generation won't overwrite it.
                Vector2Int chunkCoord = GridService.CellToChunk(cellData.X, cellData.Z);
                chunkLoader.MarkChunkLoaded(chunkCoord.x, cellData.Y, chunkCoord.y);
            }

            // Restore world objects.
            if (_worldObjectDatabase != null)
            {
                WorldObjectService objectService = bootstrapper.LogicWorld.Get<WorldObjectService>();
                ObstacleService obstacleService = bootstrapper.LogicWorld.Get<ObstacleService>();
                WorldObjectVisualSyncService visualSync = bootstrapper.VisualWorld.Get<WorldObjectVisualSyncService>();

                foreach (ObjectData objData in data.Objects)
                {
                    SpawnObjectFromData(objData, grid, objectService, obstacleService, visualSync);
                }
            }

            _levelName = data.Metadata.Name;

            // Rebuild all visible chunks.
            bootstrapper.VisualWorld.Get<WorldRenderService>().RebuildForCurrentView();
        }

        private void SpawnObjectFromData(ObjectData objData, GridService grid, WorldObjectService objectService, ObstacleService obstacleService, WorldObjectVisualSyncService visualSync)
        {
            if (!_worldObjectDatabase.TryGetByTypeId(objData.TypeId, out WorldObjectDatabase.Entry entry))
            {
                Debug.LogWarning($"[LevelEditorTools] Unknown TypeId '{objData.TypeId}' — skipping. Add it to WorldObjectDatabase.");
                return;
            }
            if (entry.Prefab == null)
            {
                Debug.LogWarning($"[LevelEditorTools] Entry '{entry.Name}' has no prefab — skipping.");
                return;
            }

            Vector3Int cellCoord = new Vector3Int(objData.X, objData.Y, objData.Z);
            Vector3 worldPos = grid.CellCenter(cellCoord);
            GameObject instance = Instantiate(entry.Prefab, worldPos, Quaternion.identity);

            // Create Logic WorldObject (same flow as PlaceWorldObject).
            WorldObjectAuthoring authoring = instance.GetComponent<WorldObjectAuthoring>();
            string objectName = authoring != null ? authoring.ObjectName : entry.Name;
            List<Vector2Int> occupiedCells = authoring != null ? authoring.OccupiedCells : new List<Vector2Int> { Vector2Int.zero };

            var obj = new WorldObject(objectName, worldPos);
            obj.SetPosition(worldPos, grid.CellSize, grid.XZOffset, objData.Y);
            obj.AddFeature(new Footprint(occupiedCells));

            objectService.Register(obj);
            objectService.OccupyCells(obj);

            ObstacleAuthoring obstacleAuthoring = instance.GetComponent<ObstacleAuthoring>();
            if (obstacleAuthoring != null)
            {
                obj.AddFeature(new Obstacle(obstacleAuthoring.BlockedCells));
                obstacleService.RegisterObstacle(obj);
            }

            MoverAuthoring moverAuthoring = instance.GetComponent<MoverAuthoring>();
            if (moverAuthoring != null)
            {
                obj.AddFeature(new Mover(moverAuthoring.MaxSpeed, moverAuthoring.Acceleration));
                visualSync.Track(obj.Id, instance.transform);
            }

            _placedObjects[cellCoord] = new PlacedObject
            {
                LogicObjectId = obj.Id,
                Visual = instance,
                TypeId = objData.TypeId,
            };
        }

        // ── Terrain Actions ──────────────────────────────────────────────────

        public void PaintTerrain()
        {
            if (!EnsurePlaying()) { return; }
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;

            GridService grid = bootstrapper.LogicWorld.Get<GridService>();
            grid.Grid.SetCell(_terrainCell, new Cell(_terrainTypeId));

            RebuildChunkAt(bootstrapper, _terrainCell);
            Debug.Log($"[LevelEditorTools] Painted terrain (id={_terrainTypeId}) at {_terrainCell}");
        }

        public void EraseTerrain()
        {
            if (!EnsurePlaying()) { return; }
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;

            GridService grid = bootstrapper.LogicWorld.Get<GridService>();
            grid.Grid.RemoveCell(_terrainCell);

            RebuildChunkAt(bootstrapper, _terrainCell);
            Debug.Log($"[LevelEditorTools] Erased terrain at {_terrainCell}");
        }

        public void DigTerrain()
        {
            if (!EnsurePlaying()) { return; }
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;

            UndergroundService underground = bootstrapper.LogicWorld.Get<UndergroundService>();
            underground.DigCell(_terrainCell);
            Debug.Log($"[LevelEditorTools] Dug cell at {_terrainCell}");
        }

        // ── World Object Actions ─────────────────────────────────────────────

        public void PlaceWorldObject()
        {
            if (!EnsurePlaying()) { return; }
            if (_worldObjectDatabase == null)
            {
                Debug.LogWarning("[LevelEditorTools] No WorldObjectDatabase assigned.");
                return;
            }
            if (!_worldObjectDatabase.TryGet(_objectDatabaseId, out WorldObjectDatabase.Entry entry))
            {
                Debug.LogWarning($"[LevelEditorTools] No entry with ID {_objectDatabaseId} in WorldObjectDatabase.");
                return;
            }
            if (entry.Prefab == null)
            {
                Debug.LogWarning($"[LevelEditorTools] Entry '{entry.Name}' (ID {entry.Id}) has no prefab.");
                return;
            }

            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            GridService grid = bootstrapper.LogicWorld.Get<GridService>();
            WorldObjectService objectService = bootstrapper.LogicWorld.Get<WorldObjectService>();
            ObstacleService obstacleService = bootstrapper.LogicWorld.Get<ObstacleService>();
            WorldObjectVisualSyncService visualSync = bootstrapper.VisualWorld.Get<WorldObjectVisualSyncService>();

            // Remove existing object at this cell if we placed one before.
            if (_placedObjects.ContainsKey(_objectCell))
            {
                RemoveTrackedObject(_objectCell, objectService, obstacleService, visualSync);
            }

            // Instantiate prefab at cell center.
            Vector3 worldPos = grid.CellCenter(_objectCell);
            GameObject instance = Instantiate(entry.Prefab, worldPos, Quaternion.identity);

            // Create Logic WorldObject.
            WorldObjectAuthoring authoring = instance.GetComponent<WorldObjectAuthoring>();
            string objectName = authoring != null ? authoring.ObjectName : entry.Name;
            List<Vector2Int> occupiedCells = authoring != null ? authoring.OccupiedCells : new List<Vector2Int> { Vector2Int.zero };

            var obj = new WorldObject(objectName, worldPos);
            obj.SetPosition(worldPos, grid.CellSize, grid.XZOffset, grid.Elevation);
            obj.AddFeature(new Footprint(occupiedCells));

            objectService.Register(obj);
            objectService.OccupyCells(obj);

            // Register obstacle if the prefab has ObstacleAuthoring.
            ObstacleAuthoring obstacleAuthoring = instance.GetComponent<ObstacleAuthoring>();
            if (obstacleAuthoring != null)
            {
                obj.AddFeature(new Obstacle(obstacleAuthoring.BlockedCells));
                obstacleService.RegisterObstacle(obj);
            }

            // Register mover if the prefab has MoverAuthoring.
            MoverAuthoring moverAuthoring = instance.GetComponent<MoverAuthoring>();
            if (moverAuthoring != null)
            {
                obj.AddFeature(new Mover(moverAuthoring.MaxSpeed, moverAuthoring.Acceleration));
                visualSync.Track(obj.Id, instance.transform);
            }

            _placedObjects[_objectCell] = new PlacedObject
            {
                LogicObjectId = obj.Id,
                Visual = instance,
                TypeId = entry.TypeId,
            };
            Debug.Log($"[LevelEditorTools] Placed '{entry.Name}' (ID {entry.Id}) at {_objectCell}");
        }

        public void RemoveWorldObject()
        {
            if (!EnsurePlaying()) { return; }
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            WorldObjectService objectService = bootstrapper.LogicWorld.Get<WorldObjectService>();
            ObstacleService obstacleService = bootstrapper.LogicWorld.Get<ObstacleService>();
            WorldObjectVisualSyncService visualSync = bootstrapper.VisualWorld.Get<WorldObjectVisualSyncService>();

            // Try removing from our own tracking first.
            if (_placedObjects.ContainsKey(_objectCell))
            {
                RemoveTrackedObject(_objectCell, objectService, obstacleService, visualSync);
                Debug.Log($"[LevelEditorTools] Removed placed object at {_objectCell}");
                return;
            }

            // Fall back to removing whatever Logic object occupies this cell.
            WorldObject obj = objectService.GetObjectAtCell(_objectCell);
            if (obj != null)
            {
                if (obj.HasFeature<Obstacle>())
                {
                    obstacleService.UnregisterObstacle(obj.Id);
                }
                visualSync.Untrack(obj.Id);
                objectService.Remove(obj.Id);
                Debug.Log($"[LevelEditorTools] Removed object '{obj.Name}' (ID {obj.Id}) at {_objectCell}");
            }
            else
            {
                Debug.LogWarning($"[LevelEditorTools] No object found at {_objectCell}");
            }
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private void ClearAllPlacedObjects()
        {
            foreach (KeyValuePair<Vector3Int, PlacedObject> kvp in _placedObjects)
            {
                if (kvp.Value.Visual != null)
                {
                    Destroy(kvp.Value.Visual);
                }
            }
            _placedObjects.Clear();
        }

        private void RemoveTrackedObject(Vector3Int cell, WorldObjectService objectService, ObstacleService obstacleService, WorldObjectVisualSyncService visualSync)
        {
            PlacedObject placed = _placedObjects[cell];

            if (objectService.TryGet(placed.LogicObjectId, out WorldObject obj))
            {
                if (obj.HasFeature<Obstacle>())
                {
                    obstacleService.UnregisterObstacle(obj.Id);
                }
                visualSync.Untrack(obj.Id);
                objectService.Remove(obj.Id);
            }

            if (placed.Visual != null)
            {
                Destroy(placed.Visual);
            }

            _placedObjects.Remove(cell);
        }

        private void RebuildChunkAt(GameBootstrapper bootstrapper, Vector3Int cellCoord)
        {
            Vector2Int chunk = GridService.CellToChunk(cellCoord.x, cellCoord.z);
            WorldRenderService worldRender = bootstrapper.VisualWorld.Get<WorldRenderService>();
            worldRender.RebuildChunk(chunk);

            // Rebuild cardinal neighbours for dual-grid edge correctness.
            worldRender.RebuildChunk(new Vector2Int(chunk.x - 1, chunk.y));
            worldRender.RebuildChunk(new Vector2Int(chunk.x + 1, chunk.y));
            worldRender.RebuildChunk(new Vector2Int(chunk.x, chunk.y - 1));
            worldRender.RebuildChunk(new Vector2Int(chunk.x, chunk.y + 1));
        }

        private static bool EnsurePlaying()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[LevelEditorTools] Only works in Play mode.");
                return false;
            }
            if (GameBootstrapper.Instance == null)
            {
                Debug.LogWarning("[LevelEditorTools] No GameBootstrapper found.");
                return false;
            }
            return true;
        }
    }
}
