using UnityEngine;

namespace Dungeon.Visuals.Lighting
{
    // Defines the vertical height profile for a sprite-based object.
    // Heights are authored in pixel rows (from the bottom of the sprite).
    // The lighting system uses this to determine per-pixel shadow casting and receiving.
    //
    // Height ramp model (pixel rows from bottom):
    //   row < HeightStartPixel  → height = GroundOffset
    //   HeightStartPixel ≤ row ≤ HeightEndPixel → height ramps linearly from GroundOffset to GroundOffset + TotalHeight
    //   row > HeightEndPixel    → height = GroundOffset + TotalHeight
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteHeightProfile : MonoBehaviour
    {
        private static readonly int OBJECT_HEIGHT_ID = Shader.PropertyToID("_ObjectHeight");
        private static readonly int OBJECT_GROUND_OFFSET_ID = Shader.PropertyToID("_ObjectGroundOffset");
        private static readonly int OBJECT_TOTAL_HEIGHT_ID = Shader.PropertyToID("_ObjectTotalHeight");
        private static readonly int HEIGHT_START_ID = Shader.PropertyToID("_HeightStart");
        private static readonly int HEIGHT_END_ID = Shader.PropertyToID("_HeightEnd");

        [Tooltip("Height of the object's base above the ground plane.")]
        [Min(0f)]
        public float GroundOffset = 0f;

        [Tooltip("Total height of the object from base to peak.")]
        [Min(0.01f)]
        public float TotalHeight = 1f;

        [Tooltip("Pixel row (from bottom of sprite) where the height ramp begins. Below this row, height = GroundOffset.")]
        [Min(0)]
        public int HeightStartPixel = 0;

        [Tooltip("Pixel row (from bottom of sprite) where the height ramp ends. Above this row, height = GroundOffset + TotalHeight.")]
        [Min(0)]
        public int HeightEndPixel = 16;

        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private float _lastGroundOffset = -1f;
        private float _lastTotalHeight = -1f;
        private float _lastHeightStart = -1f;
        private float _lastHeightEnd = -1f;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            float normalizedStart = GetNormalizedHeightStart();
            float normalizedEnd = GetNormalizedHeightEnd();

            // Only update the material property block when values change.
            if (!Mathf.Approximately(_lastGroundOffset, GroundOffset) ||
                !Mathf.Approximately(_lastTotalHeight, TotalHeight) ||
                !Mathf.Approximately(_lastHeightStart, normalizedStart) ||
                !Mathf.Approximately(_lastHeightEnd, normalizedEnd))
            {
                _spriteRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(OBJECT_HEIGHT_ID, GroundOffset + TotalHeight);
                _propertyBlock.SetFloat(OBJECT_GROUND_OFFSET_ID, GroundOffset);
                _propertyBlock.SetFloat(OBJECT_TOTAL_HEIGHT_ID, TotalHeight);
                _propertyBlock.SetFloat(HEIGHT_START_ID, normalizedStart);
                _propertyBlock.SetFloat(HEIGHT_END_ID, normalizedEnd);
                _spriteRenderer.SetPropertyBlock(_propertyBlock);

                _lastGroundOffset = GroundOffset;
                _lastTotalHeight = TotalHeight;
                _lastHeightStart = normalizedStart;
                _lastHeightEnd = normalizedEnd;
            }
        }

        // Returns the normalized (0–1) Y position where height ramp begins.
        public float GetNormalizedHeightStart()
        {
            float spriteHeightPixels = GetSpriteHeightPixels();
            if (spriteHeightPixels <= 0f) { return 0f; }
            return Mathf.Clamp01(HeightStartPixel / spriteHeightPixels);
        }

        // Returns the normalized (0–1) Y position where height ramp ends.
        public float GetNormalizedHeightEnd()
        {
            float spriteHeightPixels = GetSpriteHeightPixels();
            if (spriteHeightPixels <= 0f) { return 1f; }
            return Mathf.Clamp01(HeightEndPixel / spriteHeightPixels);
        }

        private float GetSpriteHeightPixels()
        {
            if (_spriteRenderer == null) { _spriteRenderer = GetComponent<SpriteRenderer>(); }
            if (_spriteRenderer == null || _spriteRenderer.sprite == null) { return 0f; }
            return _spriteRenderer.sprite.rect.height;
        }

        private void OnDrawGizmosSelected()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) { return; }

            Sprite sprite = sr.sprite;
            float ppu = sprite.pixelsPerUnit;
            Bounds spriteBounds = sprite.bounds;

            // Sprite local-space extents.
            float spriteMinY = spriteBounds.min.y;
            float spriteMaxY = spriteBounds.max.y;
            float spriteMinX = spriteBounds.min.x;
            float spriteMaxX = spriteBounds.max.x;
            float spriteWidth = spriteMaxX - spriteMinX;

            // Pixel rows to local Y positions.
            float heightStartY = spriteMinY + (HeightStartPixel / ppu);
            float heightEndY = spriteMinY + (HeightEndPixel / ppu);

            // Clamp to sprite bounds.
            heightStartY = Mathf.Clamp(heightStartY, spriteMinY, spriteMaxY);
            heightEndY = Mathf.Clamp(heightEndY, spriteMinY, spriteMaxY);

            // Extend lines slightly beyond sprite edges for visibility.
            float lineExtend = spriteWidth * 0.15f;
            float left = spriteMinX - lineExtend;
            float right = spriteMaxX + lineExtend;

            Matrix4x4 localToWorld = transform.localToWorldMatrix;

            // Height start line (cyan).
            Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
            Vector3 startLeft = localToWorld.MultiplyPoint3x4(new Vector3(left, heightStartY, 0f));
            Vector3 startRight = localToWorld.MultiplyPoint3x4(new Vector3(right, heightStartY, 0f));
            Gizmos.DrawLine(startLeft, startRight);

            // Height end line (yellow).
            Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
            Vector3 endLeft = localToWorld.MultiplyPoint3x4(new Vector3(left, heightEndY, 0f));
            Vector3 endRight = localToWorld.MultiplyPoint3x4(new Vector3(right, heightEndY, 0f));
            Gizmos.DrawLine(endLeft, endRight);

            // Gradient fill between start and end (semi-transparent).
            int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float t0 = (float)i / steps;
                float t1 = (float)(i + 1) / steps;
                float y0 = Mathf.Lerp(heightStartY, heightEndY, t0);
                float y1 = Mathf.Lerp(heightStartY, heightEndY, t1);

                // Gradient from cyan to yellow.
                Color c = Color.Lerp(new Color(0f, 1f, 1f, 0.15f), new Color(1f, 1f, 0f, 0.15f), (t0 + t1) * 0.5f);
                Gizmos.color = c;

                Vector3 bl = localToWorld.MultiplyPoint3x4(new Vector3(spriteMinX, y0, 0f));
                Vector3 br = localToWorld.MultiplyPoint3x4(new Vector3(spriteMaxX, y0, 0f));
                Vector3 tr = localToWorld.MultiplyPoint3x4(new Vector3(spriteMaxX, y1, 0f));
                Vector3 tl = localToWorld.MultiplyPoint3x4(new Vector3(spriteMinX, y1, 0f));

                Gizmos.DrawLine(bl, br);
                Gizmos.DrawLine(tl, tr);
            }

            // Ground offset indicator (green line at sprite bottom + small vertical bar).
            if (GroundOffset > 0f)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
                float groundOffsetScale = GroundOffset * 0.5f; // Visual scale for the offset indicator.
                Vector3 baseLeft = localToWorld.MultiplyPoint3x4(new Vector3(left, spriteMinY, 0f));
                Vector3 baseRight = localToWorld.MultiplyPoint3x4(new Vector3(right, spriteMinY, 0f));
                Gizmos.DrawLine(baseLeft, baseRight);

                // Small vertical bars at edges showing ground offset.
                Vector3 barBottomLeft = localToWorld.MultiplyPoint3x4(new Vector3(left, spriteMinY - groundOffsetScale, 0f));
                Vector3 barBottomRight = localToWorld.MultiplyPoint3x4(new Vector3(right, spriteMinY - groundOffsetScale, 0f));
                Gizmos.DrawLine(baseLeft, barBottomLeft);
                Gizmos.DrawLine(baseRight, barBottomRight);
                Gizmos.DrawLine(barBottomLeft, barBottomRight);
            }

            // Total height label bar on the right side.
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            float barX = spriteMaxX + lineExtend * 0.5f;
            Vector3 barBottom = localToWorld.MultiplyPoint3x4(new Vector3(barX, heightStartY, 0f));
            Vector3 barTop = localToWorld.MultiplyPoint3x4(new Vector3(barX, heightEndY, 0f));
            Gizmos.DrawLine(barBottom, barTop);

            // Small tick marks at top and bottom of the bar.
            float tickSize = lineExtend * 0.3f;
            Vector3 tickBottomLeft = localToWorld.MultiplyPoint3x4(new Vector3(barX - tickSize, heightStartY, 0f));
            Vector3 tickBottomRight = localToWorld.MultiplyPoint3x4(new Vector3(barX + tickSize, heightStartY, 0f));
            Vector3 tickTopLeft = localToWorld.MultiplyPoint3x4(new Vector3(barX - tickSize, heightEndY, 0f));
            Vector3 tickTopRight = localToWorld.MultiplyPoint3x4(new Vector3(barX + tickSize, heightEndY, 0f));
            Gizmos.DrawLine(tickBottomLeft, tickBottomRight);
            Gizmos.DrawLine(tickTopLeft, tickTopRight);

            // 3D height bar: vertical line from ground (Y=0) at the object's XZ position
            // showing GroundOffset and TotalHeight in world-space meters.
            Vector3 worldPos = transform.position;
            Vector3 groundPos = new Vector3(worldPos.x, 0f, worldPos.z);
            Vector3 offsetPos = new Vector3(worldPos.x, GroundOffset, worldPos.z);
            Vector3 topPos = new Vector3(worldPos.x, GroundOffset + TotalHeight, worldPos.z);

            // Ground to GroundOffset (green) — shows how far off the ground the object floats.
            if (GroundOffset > 0.001f)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
                Gizmos.DrawLine(groundPos, offsetPos);
                Gizmos.DrawLine(groundPos + new Vector3(-0.1f, 0f, 0f), groundPos + new Vector3(0.1f, 0f, 0f));
            }

            // GroundOffset to MaxHeight (orange) — the actual height of the object.
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawLine(offsetPos, topPos);
            Gizmos.DrawLine(offsetPos + new Vector3(-0.1f, 0f, 0f), offsetPos + new Vector3(0.1f, 0f, 0f));
            Gizmos.DrawLine(topPos + new Vector3(-0.1f, 0f, 0f), topPos + new Vector3(0.1f, 0f, 0f));
        }
    }
}
