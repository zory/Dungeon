using UnityEngine;

namespace Dungeon.Logic
{
    // Feature: marks a WorldObject as a point light source.
    // The light is a hard-edged circle on the XZ plane.
    public class LightSource
    {
        public float Radius { get; set; }
        public Vector2 Offset { get; set; }
        public bool Enabled { get; set; }

        public LightSource(float radius, Vector2 offset, bool enabled = true)
        {
            Radius = radius;
            Offset = offset;
            Enabled = enabled;
        }

        // World-space XZ position of the light given the owning object's world position.
        public Vector2 GetWorldPositionXZ(Vector3 objectWorldPosition)
        {
            return new Vector2(objectWorldPosition.x + Offset.x, objectWorldPosition.z + Offset.y);
        }
    }
}
