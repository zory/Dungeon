using UnityEngine;

namespace Dungeon.Logic
{
    // Feature: this WorldObject can move through the world with floating-point precision.
    // It occupies whichever cell its position falls in (FloorToInt), but moves smoothly.
    //
    // Data only — MovementService reads and writes these fields each tick.
    // External code (player input, AI) sets Direction; MovementService handles the rest.
    public class Mover
    {
        // Maximum movement speed in world units per second.
        public float MaxSpeed { get; set; }

        // How quickly velocity changes, in units/sec².
        // 0 = instant response (velocity snaps to target immediately).
        // Higher values = more inertia/sluggishness.
        public float Acceleration { get; set; }

        // Desired movement direction on the XZ plane (X, Z).
        // Magnitude should be ≤ 1. Set by external input or AI each frame.
        public Vector2 Direction { get; set; }

        // Current actual velocity on the XZ plane (X, Z).
        // Managed by MovementService — do not set externally.
        public Vector2 Velocity { get; set; }

        // Last non-zero movement direction (normalized).
        // Updated by MovementService when velocity is non-zero.
        // Used for sprite facing and interaction tiebreaking.
        // Defaults to (0, -1) = facing down.
        public Vector2 Facing { get; set; }

        public Mover(float maxSpeed, float acceleration = 0f)
        {
            MaxSpeed     = maxSpeed;
            Acceleration = acceleration;
            Direction    = Vector2.zero;
            Velocity     = Vector2.zero;
            Facing       = new Vector2(0f, -1f);
        }
    }
}
