using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals
{
    // Right-mouse drag to pan the camera ("drag the world" feel).
    // Works for both orthographic and perspective cameras.
    // Attach to any GameObject and assign the camera, or leave it empty for Camera.main.
    public class CameraDrag : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        // Used only for perspective cameras: the world Y of the plane to pan along.
        // Set this to match your GridRenderer's WorldY so panning feels 1:1.
        [SerializeField] private float _groundY = 0f;

        private Vector2 _prevMousePos;

        private void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.rightButton.wasPressedThisFrame)
                _prevMousePos = mouse.position.ReadValue();

            if (!mouse.rightButton.isPressed) return;

            Vector2 curr = mouse.position.ReadValue();

            // World point under the cursor last frame vs this frame.
            // Moving the camera by their difference keeps the grabbed point under the cursor.
            Vector3 prevWorld = ScreenToGround(_prevMousePos);
            Vector3 currWorld = ScreenToGround(curr);

            _camera.transform.position += prevWorld - currWorld;
            _prevMousePos = curr;
        }

        private Vector3 ScreenToGround(Vector2 screenPos)
        {
            if (_camera.orthographic)
            {
                // For orthographic the depth value doesn't affect XZ, any distance works.
                return _camera.ScreenToWorldPoint(
                    new Vector3(screenPos.x, screenPos.y, _camera.nearClipPlane));
            }

            // Perspective: ray → intersect with horizontal plane at _groundY.
            Ray ray = _camera.ScreenPointToRay(screenPos);
            if (Mathf.Abs(ray.direction.y) < 1e-6f)
                return _camera.transform.position;

            float t = (_groundY - ray.origin.y) / ray.direction.y;
            return t > 0f
                ? ray.origin + t * ray.direction
                : _camera.transform.position;
        }
    }
}
