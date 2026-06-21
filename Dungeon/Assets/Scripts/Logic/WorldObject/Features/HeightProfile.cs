using UnityEngine;

namespace Dungeon.Logic
{
    // Feature: defines the vertical height profile of a WorldObject.
    // Used by the lighting system to determine shadow casting/receiving per pixel row.
    //
    // Height ramp model (normalized Y from bottom of sprite, 0 = bottom, 1 = top):
    //   Y < HeightStart  → height = GroundOffset
    //   HeightStart ≤ Y ≤ HeightEnd → height ramps linearly from GroundOffset to GroundOffset + TotalHeight
    //   Y > HeightEnd    → height = GroundOffset + TotalHeight
    public class HeightProfile
    {
        // Height of the object's base above the ground plane.
        public float GroundOffset { get; set; }

        // Total height of the object from its base to its peak.
        public float TotalHeight { get; set; }

        // Normalized Y position (0–1) where the height ramp begins.
        // Below this, height is constant at GroundOffset.
        public float HeightStart { get; set; }

        // Normalized Y position (0–1) where the height ramp ends.
        // Above this, height is constant at GroundOffset + TotalHeight.
        public float HeightEnd { get; set; }

        // The maximum height of this object (base + total).
        public float MaxHeight => GroundOffset + TotalHeight;

        public HeightProfile(float groundOffset, float totalHeight, float heightStart, float heightEnd)
        {
            GroundOffset = groundOffset;
            TotalHeight = totalHeight;
            HeightStart = heightStart;
            HeightEnd = heightEnd;
        }

        // Returns the height at a given normalized Y position (0 = bottom, 1 = top).
        public float GetHeightAtNormalized(float normalizedY)
        {
            if (normalizedY <= HeightStart)
            {
                return GroundOffset;
            }
            if (normalizedY >= HeightEnd)
            {
                return GroundOffset + TotalHeight;
            }
            float t = (normalizedY - HeightStart) / (HeightEnd - HeightStart);
            return GroundOffset + TotalHeight * t;
        }
    }
}
