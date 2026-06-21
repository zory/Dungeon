using UnityEngine;

namespace Dungeon.Visuals.Lighting
{
    // Shadow caster for sprite-based objects.
    // Height determines shadow length and which shorter objects receive the shadow.
    // Also sets _ObjectHeight on the SpriteRenderer's material for shadow receiving.
    //
    // The shadow caster polygon is a configurable rectangle at the base of the object,
    // NOT the full sprite bounds. This prevents transparent sprite areas from casting shadow.
    //
    // BaseSize: width (X) and depth (Z) of the shadow caster footprint in local units.
    // BaseOffset: offset from the transform origin to the center of the footprint.
    //   Typically (0, 0) places the footprint at the object's transform position (cell center).
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteShadowCaster : MonoBehaviour
    {
        private static readonly int OBJECT_HEIGHT_ID = Shader.PropertyToID("_ObjectHeight");

        [Tooltip("Height of this sprite. Determines shadow length and height-based filtering.")]
        [Min(0.01f)]
        public float Height = 1f;

        [Tooltip("Whether this object currently casts shadows.")]
        public bool CastsShadows = true;

        [Tooltip("Width and depth of the shadow caster footprint in local units.")]
        public Vector2 BaseSize = new Vector2(1f, 0.2f);

        [Tooltip("Offset from the transform origin to the center of the footprint (local XZ).")]
        public Vector2 BaseOffset = Vector2.zero;

        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private float _lastHeight = -1f;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            // Push height to the material so the shader can compare against shadow height map.
            if (!Mathf.Approximately(_lastHeight, Height))
            {
                _spriteRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(OBJECT_HEIGHT_ID, Height);
                _spriteRenderer.SetPropertyBlock(_propertyBlock);
                _lastHeight = Height;
            }
        }

        // Returns the shadow caster polygon in local XZ space.
        // A small rectangle at the configured base position, not the full sprite bounds.
        public Vector2[] GetLocalPoints()
        {
            float halfX = BaseSize.x * 0.5f;
            float halfZ = BaseSize.y * 0.5f;
            float cx = BaseOffset.x;
            float cz = BaseOffset.y;

            return new Vector2[]
            {
                new Vector2(cx - halfX, cz - halfZ),
                new Vector2(cx + halfX, cz - halfZ),
                new Vector2(cx + halfX, cz + halfZ),
                new Vector2(cx - halfX, cz + halfZ)
            };
        }

        // Returns the polygon transformed to world-space XZ coordinates.
        public Vector2[] GetWorldPoints()
        {
            Vector2[] localPoints = GetLocalPoints();
            if (localPoints == null || localPoints.Length < 3)
            {
                return null;
            }

            Vector2[] worldPoints = new Vector2[localPoints.Length];
            for (int i = 0; i < localPoints.Length; i++)
            {
                Vector3 localPos = new Vector3(localPoints[i].x, 0f, localPoints[i].y);
                Vector3 worldPos = transform.TransformPoint(localPos);
                worldPoints[i] = new Vector2(worldPos.x, worldPos.z);
            }

            return worldPoints;
        }
    }
}
