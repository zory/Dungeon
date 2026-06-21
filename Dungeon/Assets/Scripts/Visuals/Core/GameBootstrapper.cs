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
        private System.Action<System.Collections.Generic.IReadOnlyList<UnityEngine.Vector3Int>> _onCellsRevealedHandler;

        public LogicWorld LogicWorld => _logicWorld;
        public VisualWorld VisualWorld => _visualWorld;

        private void Awake()
        {
            Instance = this;

            _logicWorld = new LogicWorld();
            _visualWorld = new VisualWorld(_logicWorld);

            CreateLogicServices();
            CreateVisualServices();

            _logicWorld.InitializeAll();
            _visualWorld.InitializeAll();

            // When underground cells are revealed, rebuild the affected visual chunks.
            WireUndergroundVisualRebuild();

            // Register world objects (static entities) and their features.
            RegisterWorldObjects();

            // Register characters and wire player input.
            RegisterCharacters();

            // If loading a saved game, populate the grid from save data.
            if (GameSession.Instance.Mode == GameSession.StartMode.LoadGame && GameSession.Instance.LoadedSaveData != null)
            {
                ApplySaveData(GameSession.Instance.LoadedSaveData);
                GameSession.Instance.LoadedSaveData = null;
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
            if (_onCellsRevealedHandler != null && _logicWorld != null)
            {
                if (_logicWorld.TryGet<UndergroundService>(out UndergroundService underground))
                {
                    underground.OnCellsRevealed -= _onCellsRevealedHandler;
                }
            }
            _visualWorld?.DisposeAll();
            _logicWorld?.DisposeAll();
            if (Instance == this) { Instance = null; }
        }

        private void WireUndergroundVisualRebuild()
        {
            UndergroundService underground = _logicWorld.Get<UndergroundService>();
            WorldRenderService worldRender = _visualWorld.Get<WorldRenderService>();
            _visualWorld.TryGet(out WallRenderService wallRender);

            _onCellsRevealedHandler = (revealedCells) =>
            {
                // Determine which chunks need visual rebuilding.
                var affectedChunks = new System.Collections.Generic.HashSet<Vector2Int>();
                foreach (Vector3Int cell in revealedCells)
                {
                    Vector2Int chunkCoord = GridService.CellToChunk(cell.x, cell.z);
                    affectedChunks.Add(chunkCoord);
                    // Rebuild cardinal neighbours for dual-grid edge correctness.
                    affectedChunks.Add(new Vector2Int(chunkCoord.x - 1, chunkCoord.y));
                    affectedChunks.Add(new Vector2Int(chunkCoord.x + 1, chunkCoord.y));
                    affectedChunks.Add(new Vector2Int(chunkCoord.x, chunkCoord.y - 1));
                    affectedChunks.Add(new Vector2Int(chunkCoord.x, chunkCoord.y + 1));
                }
                foreach (Vector2Int chunkCoord in affectedChunks)
                {
                    worldRender.RebuildChunk(chunkCoord);
                }

                // Also rebuild wall chunks when cells are revealed.
                wallRender?.RebuildForCurrentView();
            };
            underground.OnCellsRevealed += _onCellsRevealedHandler;
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
            if (GameSession.Instance.Mode == GameSession.StartMode.NewGame)
            {
                worldGenConfig.RandomizeSeed = true;
            }
            else if (GameSession.Instance.Mode == GameSession.StartMode.LoadGame && GameSession.Instance.LoadedSaveData != null)
            {
                worldGenConfig.RandomizeSeed = false;
                worldGenConfig.Seed = GameSession.Instance.LoadedSaveData.Seed;
            }

            // Extract generation rules from the terrain database.
            var worldRenderAuthoring = FindAnyObjectByType<WorldRenderAuthoring>();
            TerrainGenerationRule[] generationRules = worldRenderAuthoring != null && worldRenderAuthoring.TerrainAtlas != null
                ? worldRenderAuthoring.TerrainAtlas.GetGenerationRules()
                : System.Array.Empty<TerrainGenerationRule>();

            _logicWorld.Register(new WorldGenerationService(worldGenConfig, generationRules));

            // 3. UndergroundService — fog-of-war for underground levels
            int emptyId = worldRenderAuthoring != null && worldRenderAuthoring.TerrainAtlas != null
                ? worldRenderAuthoring.TerrainAtlas.EmptyId
                : 0;
            _logicWorld.Register(new UndergroundService(emptyId));

            // 4. ChunkLoadingService — infinite chunk streaming
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

            // 8. LightingService — global directional light + per-tile light state
            var lightingAuthoring = FindAnyObjectByType<LightingAuthoring>();
            var lightingConfig = lightingAuthoring != null ? lightingAuthoring.GetConfig() : LightingConfig.Default;
            _logicWorld.Register(new LightingService(lightingConfig));
        }

        // ── Visual Services (deterministic order) ──────────────────────────────────────

        private void CreateVisualServices()
        {
            // 1. CameraService — zoom, drag, camera state (many services depend on this)
            var cameraAuthoring = FindAnyObjectByType<CameraAuthoring>();
            Camera cam = cameraAuthoring != null ? cameraAuthoring.Camera : Camera.main;
            var cameraConfig = cameraAuthoring != null ? cameraAuthoring.GetConfig() : CameraConfig.Default;
            _visualWorld.Register(new CameraService(cameraConfig, cam));

            // 2. LightingVisualService — syncs scene MonoBehaviours to Logic lighting features
            _visualWorld.Register(new LightingVisualService());

            // 3. ElevationService — +/- input for elevation switching
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

            // 9. WorldObjectVisualSyncService — transform sync for all movers
            _visualWorld.Register(new WorldObjectVisualSyncService());

            // 10. ObstacleVisualService — colored sprites for programmatic obstacles
            {
                TerrainAtlas terrainAtlas = worldRenderAuthoring != null ? worldRenderAuthoring.TerrainAtlas : null;
                WorldObjectDatabase objectDatabase = worldRenderAuthoring != null ? worldRenderAuthoring.WorldObjectDatabase : null;
                if (terrainAtlas != null)
                {
                    _visualWorld.Register(new ObstacleVisualService(terrainAtlas, objectDatabase));
                }
                else
                {
                    Debug.LogWarning("[GameBootstrapper] TerrainAtlas not found — obstacle visuals disabled.");
                }
            }

            // 11. WallRenderService — autotiled wall mesh rendering for obstacles
            {
                TerrainAtlas terrainAtlas = worldRenderAuthoring != null ? worldRenderAuthoring.TerrainAtlas : null;
                Material wallMaterial = worldRenderAuthoring != null ? worldRenderAuthoring.WallMaterial : null;
                Transform chunkParent = worldRenderAuthoring != null ? worldRenderAuthoring.ChunkParent : null;
                if (terrainAtlas != null && wallMaterial != null)
                {
                    if (chunkParent == null)
                    {
                        chunkParent = new GameObject("WallRender_Fallback").transform;
                    }
                    _visualWorld.Register(new WallRenderService(terrainAtlas, wallMaterial, chunkParent));
                }
            }

        }

        // ── WorldObject Registration ──────────────────────────────────────────────────

        private void RegisterWorldObjects()
        {
            WorldObjectService objectService = _logicWorld.Get<WorldObjectService>();
            ObstacleService obstacleService = _logicWorld.Get<ObstacleService>();
            InteractionService interactionService = _logicWorld.Get<InteractionService>();
            GridService gridService = _logicWorld.Get<GridService>();
            WorldObjectVisualSyncService visualSync = _visualWorld.Get<WorldObjectVisualSyncService>();

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
                    visualSync.Track(obj.Id, authoring.transform);
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
            WorldObjectVisualSyncService visualSync = _visualWorld.Get<WorldObjectVisualSyncService>();

            CharacterAuthoring[] authorings = FindObjectsByType<CharacterAuthoring>(FindObjectsInactive.Exclude);
            foreach (CharacterAuthoring authoring in authorings)
            {
                CharacterConfig config = authoring.GetConfig();
                int objectId = characterService.AddCharacter(config, authoring.SpriteRenderer, authoring.transform);
                Logic.WorldObject obj = objectService.Get(objectId);

                // Position sync for all movers (characters + world objects) in one service.
                visualSync.Track(objectId, authoring.transform);

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
