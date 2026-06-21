using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Visuals.Lighting
{
    // Shadow caster for sprite-based objects.
    // Height determines shadow length and which shorter objects receive the shadow.
    // Also sets _ObjectHeight on the SpriteRenderer's material for shadow receiving.
    //
    // Shadow shape can be defined two ways:
    //   1. ShadowShape sprite assigned: the shadow polygon is derived from the sprite's
    //      non-transparent pixels (via physics shapes). Supports complex outlines and holes.
    //      The shape is scaled to fit within BaseSize and offset by BaseOffset.
    //   2. No ShadowShape: falls back to a simple rectangle defined by BaseSize/BaseOffset.
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

        [Tooltip("Optional sprite defining the shadow shape. Non-transparent pixels cast shadow. " +
                 "If null, falls back to the rectangular BaseSize/BaseOffset shape. " +
                 "The sprite must have 'Generate Physics Shape' enabled in its import settings.")]
        public Sprite ShadowShape;

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

        // Returns all shadow polygon paths in local XZ space.
        // When ShadowShape is set, extracts paths from the sprite's physics shapes
        // (supports complex outlines and holes). Otherwise returns the rectangle fallback.
        public Vector2[][] GetLocalPaths()
        {
            if (ShadowShape != null)
            {
                Vector2[][] spritePaths = ExtractPathsFromSprite();
                if (spritePaths != null)
                {
                    return spritePaths;
                }
            }

            return new Vector2[][] { GetLocalPoints() };
        }

        private Vector2[][] ExtractPathsFromSprite()
        {
            int shapeCount = ShadowShape.GetPhysicsShapeCount();
            if (shapeCount == 0) { return null; }

            Bounds spriteBounds = ShadowShape.bounds;
            float spriteWidth = spriteBounds.size.x;
            float spriteHeight = spriteBounds.size.y;

            if (spriteWidth < 0.001f || spriteHeight < 0.001f) { return null; }

            Vector2[][] paths = new Vector2[shapeCount][];
            List<Vector2> tempPoints = new List<Vector2>();

            for (int s = 0; s < shapeCount; s++)
            {
                tempPoints.Clear();
                ShadowShape.GetPhysicsShape(s, tempPoints);

                if (tempPoints.Count < 3)
                {
                    paths[s] = null;
                    continue;
                }

                Vector2[] path = new Vector2[tempPoints.Count];
                for (int i = 0; i < tempPoints.Count; i++)
                {
                    // Sprite physics shape coords are in sprite-local space (pivot-relative, in world units).
                    // Normalize to [-0.5, 0.5] based on sprite bounds, then scale by BaseSize.
                    float nx = (tempPoints[i].x - spriteBounds.center.x) / spriteWidth;
                    float ny = (tempPoints[i].y - spriteBounds.center.y) / spriteHeight;
                    path[i] = new Vector2(
                        nx * BaseSize.x + BaseOffset.x,
                        ny * BaseSize.y + BaseOffset.y);
                }
                paths[s] = path;
            }

            return paths;
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
