using System;
using System.Collections.Generic;
using Dungeon.Logic;
using Dungeon.Logic.Serialisation;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals.Services
{
    [Serializable]
    public struct EditorConfig
    {
        public string SavePath;
        public ObjectDefinitionRegistry Registry;
    }

    public class EditorService : IVisualService
    {
        public enum Tool { Paint, PlaceObject, Erase }

        private readonly EditorConfig _config;
        private VisualWorld _world;
        private GridService _grid;

        // Public editor state — exposed for inspector-driven UI or future EditorUI service.
        public Tool CurrentTool { get; set; } = Tool.Paint;
        public int SelectedTileTypeId { get; set; } = (int)TileType.Grass;
        public string SelectedObjectTypeId { get; set; } = "";
        public string LevelName { get; set; } = "unnamed";

        // Placed objects tracking
        private readonly Dictionary<Vector3Int, (string TypeId, GameObject Go)> _placedObjects = new();
        private Vector3Int? _lastActionCell;

        public EditorService(EditorConfig config)
        {
            _config = config;
        }

        public void Initialize(VisualWorld world)
        {
            _world = world;
            _grid = world.GetLogic<GridService>();
        }

        public void Tick(float deltaTime)
        {
            HandleEditorInput();
            HandleSaveLoad();
        }

        private void HandleEditorInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasReleasedThisFrame)
                _lastActionCell = null;

            if (!mouse.leftButton.isPressed) return;
            if (_grid.HoveredCell == null) return;

            Vector3Int raw   = _grid.HoveredCell.Value;
            Vector3Int coord = new Vector3Int(raw.x, _grid.Elevation, raw.z);

            if (CurrentTool == Tool.PlaceObject)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                    ExecutePlaceObject(coord);
                return;
            }

            if (coord == _lastActionCell) return;
            _lastActionCell = coord;

            switch (CurrentTool)
            {
                case Tool.Paint:
                    ExecutePaint(coord);
                    break;
                case Tool.Erase:
                    ExecuteErase(coord);
                    break;
            }
        }

        private void HandleSaveLoad()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            bool ctrl = keyboard.ctrlKey.isPressed;
            if (ctrl && keyboard.sKey.wasPressedThisFrame) Save();
            if (ctrl && keyboard.lKey.wasPressedThisFrame) Load();
        }

        private void ExecutePaint(Vector3Int coord)
        {
            _grid.Grid.SetCell(coord, new Cell(SelectedTileTypeId));
            RebuildChunkAt(coord);
        }

        private void ExecuteErase(Vector3Int coord)
        {
            _grid.Grid.RemoveCell(coord);

            if (_placedObjects.TryGetValue(coord, out var entry))
            {
                if (entry.Go != null) UnityEngine.Object.Destroy(entry.Go);
                _placedObjects.Remove(coord);
            }

            RebuildChunkAt(coord);
        }

        private void ExecutePlaceObject(Vector3Int coord)
        {
            if (string.IsNullOrEmpty(SelectedObjectTypeId))
            {
                Debug.LogWarning("[EditorService] No object type selected.");
                return;
            }

            if (_config.Registry == null || !_config.Registry.TryGet(SelectedObjectTypeId, out ObjectDefinitionRegistry.ObjectDefinition def))
            {
                Debug.LogWarning($"[EditorService] Unknown object type: '{SelectedObjectTypeId}'");
                return;
            }

            Vector3 worldPos = _grid.CellCenter(coord);

            GameObject instance = UnityEngine.Object.Instantiate(def.Prefab, worldPos, Quaternion.identity);

            // Destroy existing object at this coordinate if any.
            if (_placedObjects.TryGetValue(coord, out var existing) && existing.Go != null)
                UnityEngine.Object.Destroy(existing.Go);

            _placedObjects[coord] = (SelectedObjectTypeId, instance);
        }

        // ── Save / Load ────────────────────────────────────────────────────────────────

        private void Save()
        {
            LevelData data = BuildLevelData();
            LevelDataSerializer.Save(data, _config.SavePath);
        }

        private LevelData BuildLevelData()
        {
            LevelData data = new LevelData();
            data.Metadata.Name = LevelName;

            foreach (Vector3Int coord in _grid.Grid.GetAllCoordinates())
            {
                Cell cell = _grid.Grid.GetCell(coord);
                if (cell == null) continue;
                data.Cells.Add(new CellData
                {
                    X          = coord.x,
                    Y          = coord.y,
                    Z          = coord.z,
                    TileTypeId = cell.TileId,
                });
            }

            foreach (var (coord, entry) in _placedObjects)
            {
                data.Objects.Add(new ObjectData
                {
                    TypeId = entry.TypeId,
                    X      = coord.x,
                    Y      = coord.y,
                    Z      = coord.z,
                });
            }

            return data;
        }

        private void Load()
        {
            LevelData data = LevelDataSerializer.Load(_config.SavePath);
            if (data == null) return;

            ClearAllObjects();
            _grid.Grid.Clear();

            foreach (CellData cellData in data.Cells)
            {
                _grid.Grid.SetCell(
                    new Vector3Int(cellData.X, cellData.Y, cellData.Z),
                    new Cell(cellData.TileTypeId));
            }

            foreach (ObjectData objData in data.Objects)
                SpawnAndRegisterObject(objData);

            LevelName = data.Metadata.Name;

            _world.Get<WorldRenderService>().RebuildAll();
        }

        private void SpawnAndRegisterObject(ObjectData data)
        {
            if (_config.Registry == null || !_config.Registry.TryGet(data.TypeId, out ObjectDefinitionRegistry.ObjectDefinition def))
            {
                Debug.LogWarning($"[EditorService] Cannot spawn unknown type '{data.TypeId}' — add it to ObjectDefinitionRegistry.");
                return;
            }

            float cs     = _grid.CellSize;
            float worldX = data.X * cs + cs * 0.5f + _grid.XZOffset.x;
            float worldZ = data.Z * cs + cs * 0.5f + _grid.XZOffset.y;
            float worldY = data.Y * cs;

            GameObject instance = UnityEngine.Object.Instantiate(def.Prefab, new Vector3(worldX, worldY, worldZ), Quaternion.identity);
            Vector3Int coord    = new Vector3Int(data.X, data.Y, data.Z);
            _placedObjects[coord] = (data.TypeId, instance);
        }

        private void ClearAllObjects()
        {
            foreach (var (_, entry) in _placedObjects)
            {
                if (entry.Go != null) UnityEngine.Object.Destroy(entry.Go);
            }
            _placedObjects.Clear();
        }

        private void RebuildChunkAt(Vector3Int cellCoord)
        {
            Vector2Int chunk = GridService.CellToChunk(cellCoord.x, cellCoord.z);
            _world.Get<WorldRenderService>().RebuildChunk(chunk);
        }

        public void Dispose()
        {
            ClearAllObjects();
        }
    }
}
