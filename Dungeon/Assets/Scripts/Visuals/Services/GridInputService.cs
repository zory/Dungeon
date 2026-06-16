using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals.Services
{
    public class GridInputService : IVisualService
    {
        private VisualWorld _world;
        private GridService _grid;
        private CameraService _camera;

        public void Initialize(VisualWorld world)
        {
            _world = world;
            _grid = world.GetLogic<GridService>();
            _camera = world.Get<CameraService>();
        }

        public void Tick(float deltaTime)
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector3Int? cell = ScreenToCell(mouse.position.ReadValue());
            _grid.SetHovered(cell);

            if (mouse.leftButton.wasPressedThisFrame)
                _grid.Select(cell);
        }

        private Vector3Int? ScreenToCell(Vector2 screenPos)
        {
            Camera cam = _camera.Camera;
            Ray ray = cam.ScreenPointToRay(screenPos);

            if (Mathf.Abs(ray.direction.y) < 1e-6f) return null;

            float worldY = _grid.WorldY;
            float t = (worldY - ray.origin.y) / ray.direction.y;
            if (t < 0f) return null;

            Vector3 world = ray.origin + t * ray.direction;

            int cx = Mathf.FloorToInt((world.x - _grid.XZOffset.x) / _grid.CellSize);
            int cz = Mathf.FloorToInt((world.z - _grid.XZOffset.y) / _grid.CellSize);

            return new Vector3Int(cx, _grid.Elevation, cz);
        }

        public void Dispose() { }
    }
}
