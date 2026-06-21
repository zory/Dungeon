using UnityEngine;

namespace Dungeon.Visuals.Lighting
{
    // Marks a GameObject as a shadow caster for the custom 2D light map system.
    // Defines a polygon on the XZ plane that blocks light from reaching areas behind it.
    // If Points is empty/null and a BoxCollider is attached, the polygon is auto-derived from the collider bounds.
    // Scene data holder - the LightingVisualService reads these and creates Logic features.
    public class ShadowCaster2DCustom : MonoBehaviour
    {
        [Tooltip("Polygon vertices in local space (XZ plane). If empty and a BoxCollider is present, auto-derived from collider bounds.")]
        public Vector2[] Points;

        [Tooltip("Whether this shadow caster is currently active.")]
        public bool CastsShadows = true;

        [Tooltip("Height of this caster in meters. Shadow length is derived from height and sun elevation angle.")]
        [Min(0.01f)]
        public float Height = 1f;

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

        // Returns all polygon paths (single path for this caster type).
        public Vector2[][] GetLocalPaths()
        {
            Vector2[] points = GetLocalPoints();
            if (points == null || points.Length < 3) { return null; }
            return new Vector2[][] { points };
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

        private void OnDrawGizmosSelected()
        {
            Vector2[] points = GetLocalPoints();
            if (points == null || points.Length < 3) { return; }

            // Draw shadow polygon outline on the XZ plane.
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 a = transform.TransformPoint(new Vector3(points[i].x, 0f, points[i].y));
                Vector3 b = transform.TransformPoint(new Vector3(points[(i + 1) % points.Length].x, 0f, points[(i + 1) % points.Length].y));
                Gizmos.DrawLine(a, b);
            }

            // Draw vertical height bar at the polygon centroid.
            Vector2 centroid = Vector2.zero;
            foreach (Vector2 point in points)
            {
                centroid += point;
            }
            centroid /= points.Length;

            Vector3 groundPos = transform.TransformPoint(new Vector3(centroid.x, 0f, centroid.y));
            // Reset Y to ground level in world space.
            groundPos.y = 0f;
            Vector3 topPos = groundPos + new Vector3(0f, Height, 0f);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawLine(groundPos, topPos);

            // Tick marks.
            float tickSize = 0.1f;
            Gizmos.DrawLine(groundPos + new Vector3(-tickSize, 0f, 0f), groundPos + new Vector3(tickSize, 0f, 0f));
            Gizmos.DrawLine(topPos + new Vector3(-tickSize, 0f, 0f), topPos + new Vector3(tickSize, 0f, 0f));
        }
    }
}
