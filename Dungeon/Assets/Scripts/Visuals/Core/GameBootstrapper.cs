using Dungeon.Logic.Core;
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
        private LogicWorld _logicWorld;
        private VisualWorld _visualWorld;

        private void Awake()
        {
            _logicWorld = new LogicWorld();
            _visualWorld = new VisualWorld(_logicWorld);

            CreateLogicServices();
            CreateVisualServices();

            _logicWorld.InitializeAll();
            _visualWorld.InitializeAll();

            // Register characters after services are initialized.
            RegisterCharacters();
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
            _logicWorld.Register(new WorldGenerationService(worldGenConfig));

            // 3. ChunkLoadingService — infinite chunk streaming
            var chunkLoadingAuthoring = FindAnyObjectByType<ChunkLoadingAuthoring>();
            var chunkLoadingConfig = chunkLoadingAuthoring != null ? chunkLoadingAuthoring.GetConfig() : ChunkLoadingConfig.Default;
            _logicWorld.Register(new ChunkLoadingService(chunkLoadingConfig));

            // 4. WorldObjectService — entity registry
            _logicWorld.Register(new WorldObjectService());
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

            // 7. CharacterService — character entity creation + visual sync
            _visualWorld.Register(new CharacterService());

            // 8. EditorService — paint/erase/place tools
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

        // ── Character Registration ─────────────────────────────────────────────────────

        private void RegisterCharacters()
        {
            var characterService = _visualWorld.Get<CharacterService>();
            var authorings = FindObjectsByType<CharacterAuthoring>(FindObjectsInactive.Exclude);
            foreach (var authoring in authorings)
            {
                characterService.AddCharacter(authoring.GetConfig(), authoring.SpriteRenderer);
            }
        }
    }
}
