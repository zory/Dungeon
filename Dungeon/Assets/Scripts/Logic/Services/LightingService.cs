using System;
using System.Collections.Generic;
using Dungeon.Logic.Core;
using UnityEngine;

namespace Dungeon.Logic.Services
{
    [Serializable]
    public struct LightingConfig
    {
        public bool GlobalLightEnabled;

        [Tooltip("Azimuth direction the light comes FROM on the XZ plane (degrees).")]
        [Range(0f, 360f)]
        public float GlobalLightAngle;

        [Tooltip("Sun elevation above the horizon (degrees). 0 = horizon (very long shadows), 90 = directly overhead (no shadows).")]
        [Range(0f, 90f)]
        public float GlobalElevationAngle;

        [Tooltip("Maximum shadow distance cap to prevent infinitely long shadows at low sun angles.")]
        [Min(1f)]
        public float MaxShadowDistance;

        public static LightingConfig Default => new LightingConfig
        {
            GlobalLightEnabled = true,
            GlobalLightAngle = 225f,
            GlobalElevationAngle = 45f,
            MaxShadowDistance = 50f,
        };
    }

    // Data passed to the renderer for shadow casters.
    public struct ShadowCasterRenderData
    {
        // One or more polygon paths. For simple shapes this is a single path.
        // For shapes with holes, additional paths represent cutouts.
        public Vector2[][] WorldPaths;

        // Height profile — determines shadow length and per-pixel height.
        public float GroundOffset;
        public float TotalHeight;
        public float HeightStart; // normalized 0-1 on sprite Y axis
        public float HeightEnd;   // normalized 0-1 on sprite Y axis

        // Maximum height of this caster (GroundOffset + TotalHeight).
        public float MaxHeight => GroundOffset + TotalHeight;

        // When true, the caster polygon itself is not rendered as a solid shadow occluder -
        // only the shadow fins are drawn. Used for sprite-based casters where the sprite
        // provides visual coverage and the polygon occluder would darken transparent areas.
        public bool SkipOccluder;
    }

    // Data passed to the renderer for point lights.
    public struct PointLightRenderData
    {
        public Vector2 Position;
        public float Radius;
        public float Height;
    }

    // Logic service: global directional light state, per-tile lighting queries,
    // and render data extraction for the visual layer.
    //
    // Shadow casters and light sources are WorldObject features - this service
    // iterates registered WorldObjects via WorldObjectService.
    public class LightingService : ILogicService
    {
        private readonly LightingConfig _config;

        private bool _globalLightEnabled;
        private float _globalAngleDegrees;
        private float _globalElevationAngle;
        private float _maxShadowDistance;

        private WorldObjectService _objects;
        private GridService _grid;

        // Fired when global light direction or elevation changes.
        public event Action OnGlobalLightChanged;

        // -- Global light state ---------------------------------------------------

        public bool GlobalLightEnabled
        {
            get => _globalLightEnabled;
            set
            {
                if (_globalLightEnabled == value) { return; }
                _globalLightEnabled = value;
                OnGlobalLightChanged?.Invoke();
            }
        }

        public float GlobalAngleDegrees
        {
            get => _globalAngleDegrees;
            set
            {
                if (Mathf.Approximately(_globalAngleDegrees, value)) { return; }
                _globalAngleDegrees = value;
                OnGlobalLightChanged?.Invoke();
            }
        }

        // Sun elevation above the horizon in degrees.
        // 0 = at horizon (very long shadows), 90 = directly overhead (no shadows).
        public float GlobalElevationAngle
        {
            get => _globalElevationAngle;
            set
            {
                float clamped = Mathf.Clamp(value, 0f, 90f);
                if (Mathf.Approximately(_globalElevationAngle, clamped)) { return; }
                _globalElevationAngle = clamped;
                OnGlobalLightChanged?.Invoke();
            }
        }

        // Global cap on shadow distance to prevent infinitely long shadows at low sun angles.
        public float MaxShadowDistance => _maxShadowDistance;

        // Normalized direction vector the light comes FROM on the XZ plane.
        public Vector2 GlobalLightDirection => new Vector2(
            Mathf.Cos(_globalAngleDegrees * Mathf.Deg2Rad),
            Mathf.Sin(_globalAngleDegrees * Mathf.Deg2Rad));

        // -- Constructor ----------------------------------------------------------

        public LightingService(LightingConfig config)
        {
            _config = config;
        }

        public void Initialize(LogicWorld world)
        {
            _globalLightEnabled = _config.GlobalLightEnabled;
            _globalAngleDegrees = _config.GlobalLightAngle;
            _globalElevationAngle = _config.GlobalElevationAngle;
            _maxShadowDistance = _config.MaxShadowDistance;
            _objects = world.Get<WorldObjectService>();
            _grid = world.Get<GridService>();
        }

        public void Tick(float deltaTime) { }

        // -- Shadow length calculation --------------------------------------------

        // Computes the directional shadow length on the ground for a caster of given max height.
        // Shadow length = maxHeight / tan(elevationAngle), capped at MaxShadowDistance.
        public float GetDirectionalShadowLength(float maxHeight)
        {
            if (_globalElevationAngle >= 89.9f) { return 0f; }
            if (_globalElevationAngle <= 0.1f) { return _maxShadowDistance; }
            float tanAngle = Mathf.Tan(_globalElevationAngle * Mathf.Deg2Rad);
            return Mathf.Min(maxHeight / tanAngle, _maxShadowDistance);
        }

        // -- Render data extraction -----------------------------------------------
        // Used by the visual layer's renderer feature to build shadow/light meshes.

        public void GetShadowCasterData(List<ShadowCasterRenderData> output)
        {
            output.Clear();
            foreach (KeyValuePair<int, WorldObject> kvp in _objects.All)
            {
                WorldObject obj = kvp.Value;
                if (!obj.TryGetFeature<ShadowCaster>(out ShadowCaster caster)) { continue; }
                if (!caster.Enabled) { continue; }

                Vector2[][] worldPaths = caster.GetWorldPaths(obj.WorldPosition);
                if (worldPaths == null || worldPaths.Length == 0) { continue; }

                // Read height profile from the same WorldObject (defaults if absent).
                obj.TryGetFeature<HeightProfile>(out HeightProfile heightProfile);
                float groundOffset = heightProfile?.GroundOffset ?? 0f;
                float totalHeight = heightProfile?.TotalHeight ?? 1f;
                float heightStart = heightProfile?.HeightStart ?? 0f;
                float heightEnd = heightProfile?.HeightEnd ?? 1f;

                output.Add(new ShadowCasterRenderData
                {
                    WorldPaths = worldPaths,
                    GroundOffset = groundOffset,
                    TotalHeight = totalHeight,
                    HeightStart = heightStart,
                    HeightEnd = heightEnd,
                    SkipOccluder = caster.SkipOccluder,
                });
            }
        }

        public void GetPointLightData(List<PointLightRenderData> output)
        {
            output.Clear();
            foreach (KeyValuePair<int, WorldObject> kvp in _objects.All)
            {
                WorldObject obj = kvp.Value;
                if (!obj.TryGetFeature<LightSource>(out LightSource light)) { continue; }
                if (!light.Enabled) { continue; }

                output.Add(new PointLightRenderData
                {
                    Position = light.GetWorldPositionXZ(obj.WorldPosition),
                    Radius = light.Radius,
                    Height = light.Height,
                });
            }
        }

        // -- Per-tile lighting query ----------------------------------------------

        public TileLightState GetTileLightState(int x, int z)
        {
            float cellSize = _grid.CellSize;
            Vector2 offset = _grid.XZOffset;
            float halfCell = cellSize * 0.5f;

            // Tile center in world XZ.
            float cx = x * cellSize + halfCell + offset.x;
            float cz = z * cellSize + halfCell + offset.y;

            // Test center + 4 corners.
            int litCount = 0;
            if (IsPointLit(cx, cz)) { litCount++; }
            if (IsPointLit(cx - halfCell, cz - halfCell)) { litCount++; }
            if (IsPointLit(cx + halfCell, cz - halfCell)) { litCount++; }
            if (IsPointLit(cx - halfCell, cz + halfCell)) { litCount++; }
            if (IsPointLit(cx + halfCell, cz + halfCell)) { litCount++; }

            if (litCount == 5) { return TileLightState.FullyLit; }
            if (litCount == 0) { return TileLightState.FullyUnlit; }
            return TileLightState.PartiallyLit;
        }

        private bool IsPointLit(float px, float pz)
        {
            // Check global light.
            if (_globalElevationAngle < 89.9f)
            {
                bool inGlobalShadow = false;
                Vector2 lightDir = GlobalLightDirection;
                foreach (KeyValuePair<int, WorldObject> kvp in _objects.All)
                {
                    WorldObject obj = kvp.Value;
                    if (!obj.TryGetFeature<ShadowCaster>(out ShadowCaster caster)) { continue; }
                    if (!caster.Enabled) { continue; }

                    Vector2[][] worldPaths = caster.GetWorldPaths(obj.WorldPosition);
                    if (worldPaths == null) { continue; }

                    // Shadow length derived from height profile and sun elevation.
                    obj.TryGetFeature<HeightProfile>(out HeightProfile hp);
                    float maxHeight = hp?.MaxHeight ?? 1f;
                    float maxDist = GetDirectionalShadowLength(maxHeight);
                    if (IsPointInDirectionalShadow(px, pz, lightDir, worldPaths, maxDist))
                    {
                        inGlobalShadow = true;
                        break;
                    }
                }
                if (!inGlobalShadow) { return true; }
            }

            // Check point lights.
            foreach (KeyValuePair<int, WorldObject> kvp in _objects.All)
            {
                WorldObject obj = kvp.Value;
                if (!obj.TryGetFeature<LightSource>(out LightSource light)) { continue; }
                if (!light.Enabled) { continue; }

                Vector2 lightPos = light.GetWorldPositionXZ(obj.WorldPosition);
                float dx = px - lightPos.x;
                float dz = pz - lightPos.y;
                if (dx * dx + dz * dz > light.Radius * light.Radius) { continue; }

                // Point is within light radius - check if any caster blocks it.
                bool blocked = false;
                foreach (KeyValuePair<int, WorldObject> kvp2 in _objects.All)
                {
                    WorldObject obj2 = kvp2.Value;
                    if (!obj2.TryGetFeature<ShadowCaster>(out ShadowCaster caster)) { continue; }
                    if (!caster.Enabled) { continue; }

                    Vector2[][] worldPaths = caster.GetWorldPaths(obj2.WorldPosition);
                    if (worldPaths == null) { continue; }

                    if (IsPointBlockedFromLight(px, pz, lightPos, worldPaths))
                    {
                        blocked = true;
                        break;
                    }
                }
                if (!blocked) { return true; }
            }

            return false;
        }

        // -- 2D shadow geometry tests ---------------------------------------------

        // Tests if a point is in the directional shadow of a set of polygon paths.
        // Uses even-odd rule: counts ray-edge intersections across all paths.
        // Odd count = in shadow (handles holes correctly).
        private static bool IsPointInDirectionalShadow(
            float px, float pz, Vector2 lightDir, Vector2[][] paths, float maxDistance)
        {
            float rdx = lightDir.x;
            float rdz = lightDir.y;
            int hitCount = 0;

            for (int p = 0; p < paths.Length; p++)
            {
                Vector2[] polygon = paths[p];
                if (polygon == null || polygon.Length < 3) { continue; }

                int count = polygon.Length;
                for (int i = 0; i < count; i++)
                {
                    Vector2 a = polygon[i];
                    Vector2 b = polygon[(i + 1) % count];

                    float t = RaySegmentIntersection(px, pz, rdx, rdz, a.x, a.y, b.x, b.y);
                    if (t >= 0f && t <= maxDistance)
                    {
                        hitCount++;
                    }
                }
            }

            return (hitCount & 1) != 0;
        }

        // Tests if polygon paths block the line of sight from a point to a light position.
        // Uses even-odd rule for correct hole handling.
        private static bool IsPointBlockedFromLight(
            float px, float pz, Vector2 lightPos, Vector2[][] paths)
        {
            float rdx = lightPos.x - px;
            float rdz = lightPos.y - pz;
            float lightDist = Mathf.Sqrt(rdx * rdx + rdz * rdz);
            if (lightDist < 0.001f) { return false; }

            rdx /= lightDist;
            rdz /= lightDist;
            int hitCount = 0;

            for (int p = 0; p < paths.Length; p++)
            {
                Vector2[] polygon = paths[p];
                if (polygon == null || polygon.Length < 3) { continue; }

                int count = polygon.Length;
                for (int i = 0; i < count; i++)
                {
                    Vector2 a = polygon[i];
                    Vector2 b = polygon[(i + 1) % count];

                    float t = RaySegmentIntersection(px, pz, rdx, rdz, a.x, a.y, b.x, b.y);
                    if (t >= 0f && t < lightDist)
                    {
                        hitCount++;
                    }
                }
            }

            return (hitCount & 1) != 0;
        }

        // 2D ray-segment intersection. Returns the distance t along the ray
        // where it intersects the segment, or -1 if no intersection.
        // Ray: origin (ox, oz), direction (dx, dz).
        // Segment: (ax, az) to (bx, bz).
        private static float RaySegmentIntersection(
            float ox, float oz, float dx, float dz,
            float ax, float az, float bx, float bz)
        {
            float sx = bx - ax;
            float sz = bz - az;
            float denom = dx * sz - dz * sx;

            // Parallel - no intersection.
            if (denom > -0.0001f && denom < 0.0001f) { return -1f; }

            float t = ((ax - ox) * sz - (az - oz) * sx) / denom;
            float u = ((ax - ox) * dz - (az - oz) * dx) / denom;

            if (t >= 0f && u >= 0f && u <= 1f)
            {
                return t;
            }

            return -1f;
        }

        public void Dispose() { }
    }
}
