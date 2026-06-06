using Dungeon.Logic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals
{
    // Reads mouse input and translates screen position into grid coordinates,
    // then drives GridManager with hover and selection state.
    // Assign the same GridRenderer that is rendering the grid so Y level,
    // cell size and offset stay automatically consistent.
    public class GridInputHandler : MonoBehaviour
    {
        [SerializeField] private Camera      _camera;
        [SerializeField] private GridManager  _gridManager;
        [SerializeField] private GridRenderer _gridRenderer;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector3Int? cell = ScreenToCell(mouse.position.ReadValue());
            _gridManager.SetHovered(cell);

            if (mouse.leftButton.wasPressedThisFrame)
                _gridManager.Select(cell);
        }

        // Projects a screen-space point onto the grid's XZ plane and returns
        // the cell coordinate, or null if the ray misses the plane.
        private Vector3Int? ScreenToCell(Vector2 screenPos)
        {
            Ray ray = _camera.ScreenPointToRay(screenPos);

            // Ray parallel to the horizontal plane — no intersection
            if (Mathf.Abs(ray.direction.y) < 1e-6f) return null;

            float t = (_gridRenderer.WorldY - ray.origin.y) / ray.direction.y;
            if (t < 0f) return null; // plane is behind the camera

            Vector3 world = ray.origin + t * ray.direction;

            int cx = Mathf.FloorToInt((world.x - _gridRenderer.XZOffset.x) / _gridRenderer.CellSize);
            int cz = Mathf.FloorToInt((world.z - _gridRenderer.XZOffset.y) / _gridRenderer.CellSize);

            return new Vector3Int(cx, _gridRenderer.ElevationLayer, cz);
        }
    }
}
