using UnityEngine;

namespace Dungeon.Logic
{
    // Feature: marks a WorldObject as a shadow caster for the 2D lighting system.
    // Defines a polygon on the XZ plane that blocks light.
    public class ShadowCaster
    {
        // Polygon vertices in local space (XZ plane, relative to the object's position).
        public Vector2[] LocalPoints { get; set; }

        // Maximum shadow length in world units when global light is at full intensity.
        public float MaxShadowLength { get; set; }

        // Height of the caster. Determines shadow length and which objects get shadowed.
        // Only objects with height < this value will be affected by this shadow.
        public float Height { get; set; }

        public ShadowCaster(Vector2[] localPoints, float maxShadowLength, float height = 1f)
        {
            LocalPoints = localPoints;
            MaxShadowLength = maxShadowLength;
            Height = height;
        }

        // Returns polygon vertices transformed to world-space XZ coordinates
        // using the owning object's world position.
        public Vector2[] GetWorldPoints(Vector3 objectWorldPosition)
        {
            if (LocalPoints == null || LocalPoints.Length < 3)
            {
                return null;
            }

            Vector2[] worldPoints = new Vector2[LocalPoints.Length];
            float ox = objectWorldPosition.x;
            float oz = objectWorldPosition.z;

            for (int i = 0; i < LocalPoints.Length; i++)
            {
                worldPoints[i] = new Vector2(ox + LocalPoints[i].x, oz + LocalPoints[i].y);
            }

            return worldPoints;
        }
    }
}
