using UnityEngine;

namespace Dungeon.Logic
{
    // Feature: marks a WorldObject as a shadow caster for the 2D lighting system.
    // Defines one or more polygon paths on the XZ plane that block light.
    // Multiple paths support complex shapes with holes (outer boundary CCW, holes CW).
    // Shadow length is derived from the object's HeightProfile and the light's elevation angle.
    public class ShadowCaster
    {
        // Polygon paths in local space (XZ plane, relative to the object's position).
        // Each element is a closed polygon. For simple shapes this is a single path.
        // For shapes with holes, the first path is the outer boundary (CCW) and
        // subsequent paths are holes (CW).
        public Vector2[][] LocalPaths { get; set; }

        // Whether this shadow caster is currently active. Disabled casters are skipped
        // during shadow rendering and light queries.
        public bool Enabled { get; set; }

        // When true, the caster polygon is not rendered as a solid occluder in the light map.
        // Only the shadow fins are drawn. Used for sprite-based casters where the sprite
        // itself provides visual coverage and a solid polygon would darken transparent areas.
        public bool SkipOccluder { get; set; }

        // Constructor for multiple polygon paths.
        public ShadowCaster(Vector2[][] localPaths, bool enabled = true)
        {
            LocalPaths = localPaths;
            Enabled = enabled;
        }

        // Convenience constructor for a single polygon path.
        public ShadowCaster(Vector2[] localPoints, bool enabled = true)
            : this(localPoints != null ? new Vector2[][] { localPoints } : null, enabled)
        {
        }

        // Returns all polygon paths transformed to world-space XZ coordinates.
        public Vector2[][] GetWorldPaths(Vector3 objectWorldPosition)
        {
            if (LocalPaths == null || LocalPaths.Length == 0)
            {
                return null;
            }

            float ox = objectWorldPosition.x;
            float oz = objectWorldPosition.z;

            Vector2[][] worldPaths = new Vector2[LocalPaths.Length][];
            for (int p = 0; p < LocalPaths.Length; p++)
            {
                Vector2[] localPath = LocalPaths[p];
                if (localPath == null || localPath.Length < 3)
                {
                    worldPaths[p] = null;
                    continue;
                }

                Vector2[] worldPath = new Vector2[localPath.Length];
                for (int i = 0; i < localPath.Length; i++)
                {
                    worldPath[i] = new Vector2(ox + localPath[i].x, oz + localPath[i].y);
                }
                worldPaths[p] = worldPath;
            }

            return worldPaths;
        }

        // Returns the first valid path's world points (backward compatibility).
        public Vector2[] GetWorldPoints(Vector3 objectWorldPosition)
        {
            Vector2[][] paths = GetWorldPaths(objectWorldPosition);
            if (paths == null) { return null; }
            foreach (Vector2[] path in paths)
            {
                if (path != null && path.Length >= 3) { return path; }
            }
            return null;
        }
    }
}
