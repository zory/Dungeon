using UnityEngine;

namespace Dungeon.Visuals.Lighting
{
    // Marks a GameObject as a shadow caster for the custom 2D light map system.
    // Defines a polygon on the XZ plane that blocks light from reaching areas behind it.
    // If Points is empty/null and a BoxCollider is attached, the polygon is auto-derived from the collider bounds.
    // Scene data holder — the LightingVisualService reads these and creates Logic features.
    public class ShadowCaster2DCustom : MonoBehaviour
    {
        [Tooltip("Polygon vertices in local space (XZ plane). If empty and a BoxCollider is present, auto-derived from collider bounds.")]
        public Vector2[] Points;

        [Tooltip("Whether this shadow caster is currently active.")]
        public bool CastsShadows = true;

        [Tooltip("Maximum shadow length in world units when global light is at full intensity.")]
        [Min(0f)]
        public float MaxShadowLength = 3f;

        // Returns the polygon vertices transformed to world space on the XZ plane.
        // Each returned Vector2 represents (worldX, worldZ).
        public Vector2[] GetWorldPoints()
        {
            Vector2[] localPoints = GetLocalPoints();
            if (localPoints == null || localPoints.Length < 2)
            {
                return null;
            }

            Vector2[] worldPoints = new Vector2[localPoints.Length];
            for (int i = 0; i < localPoints.Length; i++)
            {
                // Local point is in XZ plane; convert to 3D local position (Y=0).
                Vector3 localPos = new Vector3(localPoints[i].x, 0f, localPoints[i].y);
                Vector3 worldPos = transform.TransformPoint(localPos);
                worldPoints[i] = new Vector2(worldPos.x, worldPos.z);
            }

            return worldPoints;
        }

        // Returns the local points, falling back to BoxCollider auto-generation if Points is empty.
        public Vector2[] GetLocalPoints()
        {
            if (Points != null && Points.Length >= 3)
            {
                return Points;
            }

            // Auto-generate from BoxCollider if present.
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                Vector3 center = boxCollider.center;
                Vector3 size = boxCollider.size;
                float halfX = size.x * 0.5f;
                float halfZ = size.z * 0.5f;

                // Generate a rectangle in local XZ from the collider bounds.
                return new Vector2[]
                {
                    new Vector2(center.x - halfX, center.z - halfZ),
                    new Vector2(center.x + halfX, center.z - halfZ),
                    new Vector2(center.x + halfX, center.z + halfZ),
                    new Vector2(center.x - halfX, center.z + halfZ)
                };
            }

            return null;
        }
    }
}
