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
    }
}
