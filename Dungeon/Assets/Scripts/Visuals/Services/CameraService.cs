using System;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals.Services
{
    [Serializable]
    public struct CameraConfig
    {
        public float MinZoom;
        public float MaxZoom;
        public float DefaultZoom;
        public float ZoomSpeed;

        public static CameraConfig Default => new CameraConfig
        {
            MinZoom = 1f,
            MaxZoom = 10f,
            DefaultZoom = 4f,
            ZoomSpeed = 3f,
        };
    }

    public class CameraService : IVisualService
    {
        private readonly CameraConfig _config;
        private readonly Camera _camera;
        private VisualWorld _world;
        private GridService _grid;

        public Camera Camera => _camera;
        public float CurrentZoom { get; private set; }
        public float GroundY => _grid.WorldY;
        public Vector3Int CenterTile { get; private set; }

        // Drag state
        private Vector2 _prevMousePos;

        public CameraService(CameraConfig config, Camera camera)
        {
            _config = config;
            _camera = camera;
            CurrentZoom = Mathf.Clamp(config.DefaultZoom, config.MinZoom, config.MaxZoom);
        }

        public void Initialize(VisualWorld world)
        {
            _world = world;
            _grid = world.GetLogic<GridService>();
            ApplyZoom();
        }

        public void Tick(float deltaTime)
        {
            HandleScrollInput();
            HandleDragInput();
            UpdateCenterTile();
            UpdateChunkLoaderFocus();
        }

        private void HandleScrollInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float scroll = mouse.scroll.y.ReadValue();
            if (Mathf.Abs(scroll) < 0.01f) return;

            float zoomDelta = (scroll / 120f) * _config.ZoomSpeed;
            CurrentZoom = Mathf.Clamp(CurrentZoom - zoomDelta, _config.MinZoom, _config.MaxZoom);
            ApplyZoom();
        }

        private void HandleDragInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.rightButton.wasPressedThisFrame)
                _prevMousePos = mouse.position.ReadValue();

            if (!mouse.rightButton.isPressed) return;

            Vector2 curr = mouse.position.ReadValue();
            Vector3 prevWorld = ScreenToGround(_prevMousePos);
            Vector3 currWorld = ScreenToGround(curr);

            _camera.transform.position += prevWorld - currWorld;
            _prevMousePos = curr;
        }

        private void ApplyZoom()
        {
            if (_camera.orthographic)
            {
                _camera.orthographicSize = CurrentZoom;
            }
            else
            {
                Vector3 pos = _camera.transform.position;
                Vector3 forward = _camera.transform.forward;
                float currentDist = (pos.y - GroundY) / Mathf.Abs(forward.y);
                float delta = currentDist - CurrentZoom;
                _camera.transform.position += forward * delta;
            }
        }

        // Called by ElevationService after elevation changes so zoom/drag stay consistent.
        public void SyncGroundPlane()
        {
            ApplyZoom();
        }

        private void UpdateCenterTile()
        {
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            Vector3 groundPoint;

            if (_camera.orthographic)
            {
                Vector3 worldPoint = _camera.ScreenToWorldPoint(new Vector3(screenCenter.x, screenCenter.y, _camera.nearClipPlane));
                groundPoint = new Vector3(worldPoint.x, GroundY, worldPoint.z);
            }
            else
            {
                Ray ray = _camera.ScreenPointToRay(screenCenter);
                if (Mathf.Abs(ray.direction.y) < 1e-6f) return;
                float t = (GroundY - ray.origin.y) / ray.direction.y;
                if (t <= 0f) return;
                groundPoint = ray.origin + t * ray.direction;
            }

            int elevation = _grid.Elevation;
            CenterTile = new Vector3Int(
                Mathf.FloorToInt(groundPoint.x),
                elevation,
                Mathf.FloorToInt(groundPoint.z));
        }

        private void UpdateChunkLoaderFocus()
        {
            if (_world.GetLogic<ChunkLoadingService>() is { } chunkLoader)
                chunkLoader.FocusPosition = _camera.transform.position;
        }

        private Vector3 ScreenToGround(Vector2 screenPos)
        {
            if (_camera.orthographic)
            {
                return _camera.ScreenToWorldPoint(
                    new Vector3(screenPos.x, screenPos.y, _camera.nearClipPlane));
            }

            Ray ray = _camera.ScreenPointToRay(screenPos);
            if (Mathf.Abs(ray.direction.y) < 1e-6f)
                return _camera.transform.position;

            float t = (GroundY - ray.origin.y) / ray.direction.y;
            return t > 0f
                ? ray.origin + t * ray.direction
                : _camera.transform.position;
        }

        public void Dispose() { }
    }
}
