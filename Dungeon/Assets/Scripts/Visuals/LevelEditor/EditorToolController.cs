using Dungeon.Logic;
using Dungeon.Logic.Serialisation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals
{
    // Central controller for the level editor.
    //
    // Tools:
    //   Paint        — hold left-mouse and drag to paint the selected tile type.
    //   PlaceObject  — single left-click to place/replace the selected object type.
    //   Erase        — hold left-mouse and drag to remove tiles and objects.
    //
    // Keyboard:
    //   Ctrl+S        — save level to _savePath
    //   Ctrl+L        — load level from _savePath
    //   PageUp        — switch to next higher elevation layer
    //   PageDown      — switch to next lower elevation layer
    //
    // The GridManager.Grid is the authoritative cell store; EditorState tracks placed objects.
    // On save, cells are read from the Grid and objects from EditorState.
    public class EditorToolController : MonoBehaviour
    {
        [SerializeField] private GridManager             _gridManager;
        [SerializeField] private GridRenderer            _gridRenderer;
        [SerializeField] private WorldRenderer           _worldRenderer;
        [SerializeField] private EditorState             _editorState;
        [SerializeField] private ObjectDefinitionRegistry _registry;

        [Header("Save / Load")]
        [SerializeField] private string _savePath = "Assets/Levels/level.json";

        // Tracks the last cell painted this drag so we don't repaint every frame when stationary.
        private Vector3Int? _lastActionCell;

        private void Update()
        {
            HandleElevationSwitch();
            HandleEditorInput();
            HandleSaveLoad();
        }

        // ── Input ──────────────────────────────────────────────────────────────────────

        private void HandleEditorInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Reset drag tracking on button release so re-clicking the same cell works.
            if (mouse.leftButton.wasReleasedThisFrame)
                _lastActionCell = null;

            if (!mouse.leftButton.isPressed) return;
            if (_gridManager.HoveredCell == null) return;

            // Override the elevation from GridRenderer with the editor's active elevation.
            Vector3Int raw   = _gridManager.HoveredCell.Value;
            Vector3Int coord = new Vector3Int(raw.x, _editorState.ActiveElevation, raw.z);

            // For PlaceObject only act on the initial press so you don't spam-place while dragging.
            if (_editorState.CurrentTool == EditorState.Tool.PlaceObject)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                    ExecutePlaceObject(coord);
                return;
            }

            // Paint and Erase: continue acting as long as the cell changes.
            if (coord == _lastActionCell) return;
            _lastActionCell = coord;

            switch (_editorState.CurrentTool)
            {
                case EditorState.Tool.Paint:
                    ExecutePaint(coord);
                    break;
                case EditorState.Tool.Erase:
                    ExecuteErase(coord);
                    break;
            }
        }

        private void HandleElevationSwitch()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.pageUpKey.wasPressedThisFrame)
                ApplyElevation(_editorState.ActiveElevation + 1);
            else if (keyboard.pageDownKey.wasPressedThisFrame)
                ApplyElevation(_editorState.ActiveElevation - 1);
        }

        private void HandleSaveLoad()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            bool ctrl = keyboard.ctrlKey.isPressed;
            if (ctrl && keyboard.sKey.wasPressedThisFrame) Save();
            if (ctrl && keyboard.lKey.wasPressedThisFrame) Load();
        }

        // ── Tool actions ───────────────────────────────────────────────────────────────

        private void ExecutePaint(Vector3Int coord)
        {
            _gridManager.Grid.SetCell(coord, new Cell(_editorState.SelectedTileTypeId));
            RebuildChunkAt(coord);
        }

        private void ExecuteErase(Vector3Int coord)
        {
            _gridManager.Grid.RemoveCell(coord);

            if (_editorState.TryEraseObject(coord, out GameObject go))
                Destroy(go);

            RebuildChunkAt(coord);
        }

        private void ExecutePlaceObject(Vector3Int coord)
        {
            if (string.IsNullOrEmpty(_editorState.SelectedObjectTypeId))
            {
                Debug.LogWarning("[EditorToolController] No object type selected.");
                return;
            }

            if (!_registry.TryGet(_editorState.SelectedObjectTypeId, out ObjectDefinitionRegistry.ObjectDefinition def))
            {
                Debug.LogWarning($"[EditorToolController] Unknown object type: '{_editorState.SelectedObjectTypeId}'");
                return;
            }

            // Compute world-space centre of the target cell.
            float cs     = _gridRenderer.CellSize;
            float worldX = coord.x * cs + cs * 0.5f + _gridRenderer.XZOffset.x;
            float worldZ = coord.z * cs + cs * 0.5f + _gridRenderer.XZOffset.y;
            float worldY = _gridRenderer.WorldY;

            GameObject instance = Instantiate(def.Prefab, new Vector3(worldX, worldY, worldZ), Quaternion.identity);
            _editorState.PlaceObject(coord, _editorState.SelectedObjectTypeId, instance);
        }

        // ── Elevation ─────────────────────────────────────────────────────────────────

        private void ApplyElevation(int elevation)
        {
            _editorState.ActiveElevation = elevation;
            _gridRenderer.SetElevation(elevation);
            _worldRenderer.RebuildAll();
            Debug.Log($"[EditorToolController] Elevation → {elevation}");
        }

        // ── Save / Load ────────────────────────────────────────────────────────────────

        private void Save()
        {
            LevelData data = BuildLevelData();
            LevelDataSerializer.Save(data, _savePath);
        }

        private LevelData BuildLevelData()
        {
            LevelData data = new LevelData();
            data.Metadata.Name = _editorState.LevelName;

            // Cells — read directly from the grid (authoritative source).
            foreach (Vector3Int coord in _gridManager.Grid.GetAllCoordinates())
            {
                Cell cell = _gridManager.Grid.GetCell(coord);
                if (cell == null) continue;
                data.Cells.Add(new CellData
                {
                    X          = coord.x,
                    Y          = coord.y,
                    Z          = coord.z,
                    TileTypeId = cell.TileId,
                });
            }

            // Objects — tracked by EditorState.
            data.Objects.AddRange(_editorState.BuildObjectDataList());

            return data;
        }

        private void Load()
        {
            LevelData data = LevelDataSerializer.Load(_savePath);
            if (data == null) return;

            // Clear existing scene state.
            _editorState.ClearAllObjects();
            _gridManager.Grid.Clear();

            // Populate grid from saved cells.
            foreach (CellData cellData in data.Cells)
            {
                _gridManager.Grid.SetCell(
                    new Vector3Int(cellData.X, cellData.Y, cellData.Z),
                    new Cell(cellData.TileTypeId));
            }

            // Spawn and register objects.
            foreach (ObjectData objData in data.Objects)
                SpawnAndRegisterObject(objData);

            // Sync editor state metadata.
            _editorState.LevelName = data.Metadata.Name;

            // Rebuild all visible chunks for the active elevation.
            _worldRenderer.RebuildAll();
        }

        private void SpawnAndRegisterObject(ObjectData data)
        {
            if (!_registry.TryGet(data.TypeId, out ObjectDefinitionRegistry.ObjectDefinition def))
            {
                Debug.LogWarning($"[EditorToolController] Cannot spawn unknown type '{data.TypeId}' — add it to ObjectDefinitionRegistry.");
                return;
            }

            float cs     = _gridRenderer.CellSize;
            float worldX = data.X * cs + cs * 0.5f + _gridRenderer.XZOffset.x;
            float worldZ = data.Z * cs + cs * 0.5f + _gridRenderer.XZOffset.y;
            float worldY = data.Y * cs; // elevation * cell size = world Y for that layer

            GameObject   instance = Instantiate(def.Prefab, new Vector3(worldX, worldY, worldZ), Quaternion.identity);
            Vector3Int   coord    = new Vector3Int(data.X, data.Y, data.Z);
            _editorState.RegisterLoadedObject(coord, data.TypeId, instance);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private void RebuildChunkAt(Vector3Int cellCoord)
        {
            int chunkX = Mathf.FloorToInt((float)cellCoord.x / ChunkRenderer.ChunkSize);
            int chunkZ = Mathf.FloorToInt((float)cellCoord.z / ChunkRenderer.ChunkSize);
            _worldRenderer.RebuildChunk(new Vector2Int(chunkX, chunkZ));
        }
    }
}
