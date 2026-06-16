using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals.Services
{
    public class ElevationService : IVisualService
    {
        private VisualWorld _world;
        private GridService _grid;

        public void Initialize(VisualWorld world)
        {
            _world = world;
            _grid = world.GetLogic<GridService>();
        }

        public void Tick(float deltaTime)
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
                ChangeElevation(1);
            else if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
                ChangeElevation(-1);
        }

        public void ChangeElevation(int delta)
        {
            SetElevation(_grid.Elevation + delta);
        }

        public void SetElevation(int elevation)
        {
            if (elevation == _grid.Elevation) return;
            _grid.Elevation = elevation;

            // Ensure chunk data exists at the new elevation before rebuilding visuals.
            var chunkLoader = _world.GetLogic<ChunkLoadingService>();
            chunkLoader.EnsureChunksAtCurrentElevation();

            // Sync camera ground plane.
            var camera = _world.Get<CameraService>();
            camera.SyncGroundPlane();

            // Rebuild all visible chunks at the new elevation.
            var worldRender = _world.Get<WorldRenderService>();
            worldRender.RebuildAll();

            Debug.Log($"[ElevationService] Elevation → {elevation}");
        }

        public void Dispose() { }
    }
}
