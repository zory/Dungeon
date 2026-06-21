using UnityEngine;

namespace Dungeon.Visuals.Lighting
{
    // Shadow caster that derives its polygon from the attached SpriteRenderer bounds.
    // Height determines both shadow length and which objects this shadow can affect
    // (only objects shorter than this caster will be shadowed).
    // Also sets _ObjectHeight on the SpriteRenderer's material for shadow receiving.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteShadowCaster : MonoBehaviour
    {
        private static readonly int OBJECT_HEIGHT_ID = Shader.PropertyToID("_ObjectHeight");

        [Tooltip("Height of this sprite. Determines shadow length and height-based filtering.")]
        [Min(0.01f)]
        public float Height = 1f;

        [Tooltip("Whether this object currently casts shadows.")]
        public bool CastsShadows = true;

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

        // Returns the shadow caster polygon in local XZ space, derived from sprite bounds.
        // The polygon represents the base footprint of the object on the ground.
        public Vector2[] GetLocalPoints()
        {
            if (_spriteRenderer == null || _spriteRenderer.sprite == null)
            {
                return null;
            }

            Bounds bounds = _spriteRenderer.sprite.bounds;
            float halfX = bounds.extents.x;
            // Use a thin strip at the bottom of the sprite as the shadow base.
            float baseZ = bounds.min.y;
            float topZ = bounds.max.y;

            // Full sprite width, thin depth footprint for top-down shadow projection.
            return new Vector2[]
            {
                new Vector2(-halfX, baseZ),
                new Vector2(halfX, baseZ),
                new Vector2(halfX, topZ),
                new Vector2(-halfX, topZ)
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
