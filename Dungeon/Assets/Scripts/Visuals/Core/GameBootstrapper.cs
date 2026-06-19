using Dungeon.Logic;
using Dungeon.Logic.Core;
using Dungeon.Logic.Serialisation;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Authoring;
using Dungeon.Visuals.Core;
using Dungeon.Visuals.Services;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Single entry point for the game. Creates all services, owns the tick loop.
    // No other MonoBehaviour should run game logic — only Authoring components for inspector config.
    public class GameBootstrapper : MonoBehaviour
    {
        public static GameBootstrapper Instance { get; private set; }

        private LogicWorld _logicWorld;
        private VisualWorld _visualWorld;

        public LogicWorld LogicWorld => _logicWorld;

        private void Awake()
        {
            Instance = this;

            _logicWorld = new LogicWorld();
            _visualWorld = new VisualWorld(_logicWorld);

            CreateLogicServices();
            CreateVisualServices();

            _logicWorld.InitializeAll();
            _visualWorld.InitializeAll();

            // Register world objects (static entities) and their features.
            RegisterWorldObjects();

            // Register characters and wire player input.
            RegisterCharacters();

            // If loading a saved game, populate the grid from save data.
            if (GameSession.Mode == GameSession.StartMode.LoadGame && GameSession.LoadedSaveData != null)
            {
                ApplySaveData(GameSession.LoadedSaveData);
                GameSession.LoadedSaveData = null;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _logicWorld.TickAll(dt);
            _visualWorld.TickAll(dt);
        }

        private void OnDestroy()
        {
            _visualWorld?.DisposeAll();
            _logicWorld?.DisposeAll();
            if (Instance == this) { Instance = null; }
        }

        // ── Logic Services (deterministic order) ───────────────────────────────────────

        private void CreateLogicServices()
        {
            // 1. GridService — single source of truth for cellSize, xzOffset, elevation
            var gridAuthoring = FindAnyObjectByType<GridAuthoring>();
            var gridConfig = gridAuthoring != null ? gridAuthoring.GetConfig() : GridConfig.Default;
            _logicWorld.Register(new GridService(gridConfig));

            // 2. WorldGenerationService — procedural terrain
            var worldGenAuthoring = FindAnyObjectByType<WorldGenerationAuthoring>();
            var worldGenConfig = worldGenAuthoring != null ? worldGenAuthoring.GetConfig() : WorldGenerationConfig.Default;

            // Override seed based on GameSession mode.
            if (GameSession.Mode == GameSession.StartMode.NewGame)
            {
                worldGenConfig.RandomizeSeed = true;
            }
            else if (GameSession.Mode == GameSession.StartMode.LoadGame && GameSession.LoadedSaveData != null)
            {
                worldGenConfig.RandomizeSeed = false;
                worldGenConfig.Seed = GameSession.LoadedSaveData.Seed;
            }

            _logicWorld.Register(new WorldGenerationService(worldGenConfig));

            // 3. ChunkLoadingService — infinite chunk streaming
            var chunkLoadingAuthoring = FindAnyObjectByType<ChunkLoadingAuthoring>();
            var chunkLoadingConfig = chunkLoadingAuthoring != null ? chunkLoadingAuthoring.GetConfig() : ChunkLoadingConfig.Default;
            _logicWorld.Register(new ChunkLoadingService(chunkLoadingConfig));

            // 4. WorldObjectService — entity registry + cell occupancy
            _logicWorld.Register(new WorldObjectService());

            // 5. ObstacleService — tracks impassable cells
            _logicWorld.Register(new ObstacleService());

            // 6. InteractionService — tracks interactable cells, resolves interactions
            _logicWorld.Register(new InteractionService());

            // 7. MovementService — processes all movers, resolves obstacle collision
            _logicWorld.Register(new MovementService());
        }

        // ── Visual Services (deterministic order) ──────────────────────────────────────

        private void CreateVisualServices()
        {
            // 1. CameraService — zoom, drag, camera state (many services depend on this)
            var cameraAuthoring = FindAnyObjectByType<CameraAuthoring>();
            Camera cam = cameraAuthoring != null ? cameraAuthoring.Camera : Camera.main;
            var cameraConfig = cameraAuthoring != null ? cameraAuthoring.GetConfig() : CameraConfig.Default;
            _visualWorld.Register(new CameraService(cameraConfig, cam));

            // 2. ElevationService — +/- input for elevation switching
            _visualWorld.Register(new ElevationService());

            // 3. GridRenderService — GL grid lines
            var gridRenderAuthoring = FindAnyObjectByType<GridRenderAuthoring>();
            var gridRenderConfig = gridRenderAuthoring != null ? gridRenderAuthoring.GetConfig() : GridRenderConfig.Default;
            _visualWorld.Register(new GridRenderService(gridRenderConfig));

            // 4. WorldRenderService — chunk mesh rendering
            var worldRenderAuthoring = FindAnyObjectByType<WorldRenderAuthoring>();
            if (worldRenderAuthoring != null)
            {
                _visualWorld.Register(new WorldRenderService(worldRenderAuthoring.GetConfig(), worldRenderAuthoring.ChunkParent));
            }
            else
            {
                Debug.LogWarning("[GameBootstrapper] WorldRenderAuthoring not found — world chunks will not render.");
                var fallbackParent = new GameObject("WorldRender_Fallback").transform;
                _visualWorld.Register(new WorldRenderService(new WorldRenderConfig(), fallbackParent));
            }

            // 5. GridInputService — mouse-to-grid raycasting
            _visualWorld.Register(new GridInputService());

            // 6. GridHighlightService — hover/selected sprites
            var highlightAuthoring = FindAnyObjectByType<GridHighlightAuthoring>();
            var highlightConfig = highlightAuthoring != null ? highlightAuthoring.GetConfig() : GridHighlightConfig.Default;
            _visualWorld.Register(new GridHighlightService(highlightConfig));

            // 7. PlayerInputService — keyboard input for the controlled object
            _visualWorld.Register(new PlayerInputService());

            // 8. CharacterService — character entity creation + visual sync
            _visualWorld.Register(new CharacterService());

            // 9. EditorService — paint/erase/place tools
            var editorAuthoring = FindAnyObjectByType<EditorAuthoring>();
            if (editorAuthoring != null)
            {
                _visualWorld.Register(new EditorService(editorAuthoring.GetConfig()));
            }
            else
            {
                _visualWorld.Register(new EditorService(new EditorConfig { SavePath = "Assets/Levels/level.json" }));
            }
        }

        // ── WorldObject Registration ──────────────────────────────────────────────────

        private void RegisterWorldObjects()
        {
            WorldObjectService objectService = _logicWorld.Get<WorldObjectService>();
            ObstacleService obstacleService = _logicWorld.Get<ObstacleService>();
            InteractionService interactionService = _logicWorld.Get<InteractionService>();
            GridService gridService = _logicWorld.Get<GridService>();

            WorldObjectAuthoring[] authorings = FindObjectsByType<WorldObjectAuthoring>(FindObjectsInactive.Exclude);
            foreach (WorldObjectAuthoring authoring in authorings)
            {
                // Create logic entity from authoring data.
                Vector3 worldPos = authoring.transform.position;
                var obj = new Logic.WorldObject(authoring.ObjectName, worldPos);
                obj.SetPosition(worldPos, gridService.CellSize, gridService.XZOffset, gridService.Elevation);

                // Footprint — which cells this object occupies.
                obj.AddFeature(new Logic.Footprint(authoring.OccupiedCells));

                objectService.Register(obj);
                objectService.OccupyCells(obj);

                // If this object has a Mover, it can move.
                MoverAuthoring moverAuthoring = authoring.GetComponent<MoverAuthoring>();
                if (moverAuthoring != null)
                {
                    obj.AddFeature(new Logic.Mover(moverAuthoring.MaxSpeed, moverAuthoring.Acceleration));
                }

                // If this object is an obstacle, register blocked cells.
                ObstacleAuthoring obstacleAuthoring = authoring.GetComponent<ObstacleAuthoring>();
                if (obstacleAuthoring != null)
                {
                    obj.AddFeature(new Logic.Obstacle(obstacleAuthoring.BlockedCells));
                    obstacleService.RegisterObstacle(obj);
                }

                // Wire interactable and interactor from authoring components.
                WireInteractable(obj, authoring.gameObject, interactionService);
                WireInteractor(obj, authoring.gameObject);
            }
        }

        // ── Character Registration ─────────────────────────────────────────────────────

        private void RegisterCharacters()
        {
            CharacterService characterService = _visualWorld.Get<CharacterService>();
            PlayerInputService playerInput = _visualWorld.Get<PlayerInputService>();
            WorldObjectService objectService = _logicWorld.Get<WorldObjectService>();
            InteractionService interactionService = _logicWorld.Get<InteractionService>();

            CharacterAuthoring[] authorings = FindObjectsByType<CharacterAuthoring>(FindObjectsInactive.Exclude);
            foreach (CharacterAuthoring authoring in authorings)
            {
                CharacterConfig config = authoring.GetConfig();
                int objectId = characterService.AddCharacter(config, authoring.SpriteRenderer);
                Logic.WorldObject obj = objectService.Get(objectId);

                // Wire interactable and interactor from authoring components on the character prefab.
                WireInteractable(obj, authoring.gameObject, interactionService);
                WireInteractor(obj, authoring.gameObject);

                // Wire the first player-controlled character to the input handler.
                if (config.IsPlayerControlled && playerInput.ControlledObjectId < 0)
                {
                    playerInput.SetControlledObject(objectId);
                }
            }
        }

        // ── Interaction Wiring Helpers ──────────────────────────────────────────────────

        private static void WireInteractable(Logic.WorldObject obj, GameObject go, InteractionService interactionService)
        {
            InteractableAuthoring interactableAuthoring = go.GetComponent<InteractableAuthoring>();
            if (interactableAuthoring == null) { return; }

            var interactable = new Logic.Interactable(interactableAuthoring.InteractableCells, interactableAuthoring.AllowSelfInteraction);
            foreach (InteractableAuthoring.InteractableEntryData entry in interactableAuthoring.Entries)
            {
                System.Action callback = entry.Callback != null ? entry.Callback.Execute : null;
                interactable.AddEntry(entry.TypeId, callback);
            }
            obj.AddFeature(interactable);
            interactionService.RegisterInteractable(obj);
        }

        private static void WireInteractor(Logic.WorldObject obj, GameObject go)
        {
            InteractorAuthoring interactorAuthoring = go.GetComponent<InteractorAuthoring>();
            if (interactorAuthoring == null) { return; }

            var interactor = new Logic.Interactor();
            foreach (InteractorAuthoring.InteractorEntryData entry in interactorAuthoring.Entries)
            {
                System.Action callback = entry.Callback != null ? entry.Callback.Execute : null;
                interactor.AddEntry(entry.TypeId, callback);
            }
            obj.AddFeature(interactor);
        }

        // ── Save Data ──────────────────────────────────────────────────────────────────

        private void ApplySaveData(SaveData saveData)
        {
            GridService grid = _logicWorld.Get<GridService>();
            ChunkLoadingService chunkLoader = _logicWorld.Get<ChunkLoadingService>();

            grid.Grid.Clear();
            chunkLoader.Reset();

            foreach (CellData cellData in saveData.Cells)
            {
                Vector3Int coord = new Vector3Int(cellData.X, cellData.Y, cellData.Z);
                grid.Grid.SetCell(coord, new Cell(cellData.TileTypeId));

                // Mark the chunk containing this cell as loaded so it won't be regenerated.
                Vector2Int chunkCoord = GridService.CellToChunk(cellData.X, cellData.Z);
                chunkLoader.MarkChunkLoaded(chunkCoord.x, cellData.Y, chunkCoord.y);
            }

            // Rebuild visual chunks around camera.
            _visualWorld.Get<WorldRenderService>().RebuildForCurrentView();
            Debug.Log($"[GameBootstrapper] Loaded save data: {saveData.Cells.Count} cells, seed={saveData.Seed}");
        }
    }
}
