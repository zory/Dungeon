using UnityEngine;

namespace Dungeon.Visuals.Lighting
{
    // Marks a GameObject as a light source for the custom 2D light map system.
    // The light is a hard-edged circle on the XZ plane (Y is ignored for 2D positioning).
    // Scene data holder — the LightingVisualService reads these and creates Logic features.
    public class LightSource : MonoBehaviour
    {
        [Tooltip("Radius of the light circle in world units.")]
        [Min(0.01f)]
        public float Radius = 5f;

        [Tooltip("Offset from the transform position on the XZ plane.")]
        public Vector2 Offset = Vector2.zero;

        [Tooltip("Height of this light source in meters above the ground plane. Determines shadow geometry from point light.")]
        [Min(0.01f)]
        public float Height = 3f;

        [Tooltip("Whether this light source is currently active.")]
        [SerializeField] private bool _enabled = true;

        // Runtime enable/disable toggle.
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        // World-space position of the light on the XZ plane (Y component is Z in world space).
        public Vector2 WorldPositionXZ
        {
            get
            {
                Vector3 pos = transform.position;
                return new Vector2(pos.x + Offset.x, pos.z + Offset.y);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;
            // Ground-level position of the light on the XZ plane.
            Vector3 groundCenter = new Vector3(pos.x + Offset.x, 0f, pos.z + Offset.y);

            // Draw the light radius circle on the XZ ground plane.
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.8f);
            DrawCircleXZ(groundCenter, Radius, 48);

            // Inner ring at half radius for visual reference.
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.3f);
            DrawCircleXZ(groundCenter, Radius * 0.5f, 32);

            // Vertical line from ground cell position to the light's actual elevated position.
            // Shows that the light is logically at this XZ cell but elevated in Y.
            Vector3 elevatedPos = groundCenter + new Vector3(0f, Height, 0f);
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.9f);
            Gizmos.DrawLine(groundCenter, elevatedPos);

            // Small sphere at the elevated position to mark the light's 3D position.
            Gizmos.DrawWireSphere(elevatedPos, 0.08f);

            // Tick marks on the height line.
            float tickSize = 0.1f;
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.6f);
            Gizmos.DrawLine(groundCenter + new Vector3(-tickSize, 0f, 0f), groundCenter + new Vector3(tickSize, 0f, 0f));
            Gizmos.DrawLine(elevatedPos + new Vector3(-tickSize, 0f, 0f), elevatedPos + new Vector3(tickSize, 0f, 0f));
        }

        private static void DrawCircleXZ(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
