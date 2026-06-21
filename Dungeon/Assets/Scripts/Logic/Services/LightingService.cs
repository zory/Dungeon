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

        [Range(0f, 360f)]
        public float GlobalLightAngle;

        [Range(0f, 1f)]
        public float GlobalLightIntensity;

        public static LightingConfig Default => new LightingConfig
        {
            GlobalLightEnabled = true,
            GlobalLightAngle = 225f,
            GlobalLightIntensity = 0.8f,
        };
    }

    // Data passed to the renderer for shadow casters.
    public struct ShadowCasterRenderData
    {
        public Vector2[] WorldPoints;
        public float MaxShadowLength;
        public float Height;
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
    // Shadow casters and light sources are WorldObject features — this service
    // iterates registered WorldObjects via WorldObjectService.
    public class LightingService : ILogicService
    {
        private readonly LightingConfig _config;

        private bool _globalLightEnabled;
        private float _globalAngleDegrees;
        private float _globalIntensity;

        private WorldObjectService _objects;
        private GridService _grid;

        // Fired when global light direction or intensity changes.
        public event Action OnGlobalLightChanged;

        // ── Global light state ───────────────────────────────────────────────

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

        public float GlobalIntensity
        {
            get => _globalIntensity;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(_globalIntensity, clamped)) { return; }
                _globalIntensity = clamped;
                OnGlobalLightChanged?.Invoke();
            }
        }

        // Normalized direction vector the light comes FROM on the XZ plane.
        public Vector2 GlobalLightDirection => new Vector2(
            Mathf.Cos(_globalAngleDegrees * Mathf.Deg2Rad),
            Mathf.Sin(_globalAngleDegrees * Mathf.Deg2Rad));

        // ── Constructor ──────────────────────────────────────────────────────

        public LightingService(LightingConfig config)
        {
            _config = config;
        }

        public void Initialize(LogicWorld world)
        {
            _globalLightEnabled = _config.GlobalLightEnabled;
            _globalAngleDegrees = _config.GlobalLightAngle;
            _globalIntensity = _config.GlobalLightIntensity;
            _objects = world.Get<WorldObjectService>();
            _grid = world.Get<GridService>();
        }

        public void Tick(float deltaTime) { }

        // ── Render data extraction ───────────────────────────────────────────
        // Used by the visual layer's renderer feature to build shadow/light meshes.

        public void GetShadowCasterData(List<ShadowCasterRenderData> output)
        {
            output.Clear();
            foreach (KeyValuePair<int, WorldObject> kvp in _objects.All)
            {
                WorldObject obj = kvp.Value;
                if (!obj.TryGetFeature<ShadowCaster>(out ShadowCaster caster)) { continue; }

                Vector2[] worldPoints = caster.GetWorldPoints(obj.WorldPosition);
                if (worldPoints == null || worldPoints.Length < 3) { continue; }

                output.Add(new ShadowCasterRenderData
                {
                    WorldPoints = worldPoints,
                    MaxShadowLength = caster.MaxShadowLength,
                    Height = caster.Height,
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

        // ── Per-tile lighting query ──────────────────────────────────────────

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
            if (_globalIntensity > 0f)
            {
                bool inGlobalShadow = false;
                Vector2 lightDir = GlobalLightDirection;
                foreach (KeyValuePair<int, WorldObject> kvp in _objects.All)
                {
                    WorldObject obj = kvp.Value;
                    if (!obj.TryGetFeature<ShadowCaster>(out ShadowCaster caster)) { continue; }

                    Vector2[] worldPoints = caster.GetWorldPoints(obj.WorldPosition);
                    if (worldPoints == null || worldPoints.Length < 3) { continue; }

                    float maxDist = caster.MaxShadowLength * _globalIntensity;
                    if (IsPointInDirectionalShadow(px, pz, lightDir, worldPoints, maxDist))
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

                // Point is within light radius — check if any caster blocks it.
                bool blocked = false;
                foreach (KeyValuePair<int, WorldObject> kvp2 in _objects.All)
                {
                    WorldObject obj2 = kvp2.Value;
                    if (!obj2.TryGetFeature<ShadowCaster>(out ShadowCaster caster)) { continue; }

                    Vector2[] worldPoints = caster.GetWorldPoints(obj2.WorldPosition);
                    if (worldPoints == null || worldPoints.Length < 3) { continue; }

                    if (IsPointBlockedFromLight(px, pz, lightPos, worldPoints))
                    {
                        blocked = true;
                        break;
                    }
                }
                if (!blocked) { return true; }
            }

            return false;
        }

        // ── 2D shadow geometry tests ─────────────────────────────────────────

        // Tests if a point is in the directional shadow of a polygon.
        // Casts a ray from the point toward the light. If it hits the polygon
        // within maxDistance, the point is in shadow.
        private static bool IsPointInDirectionalShadow(
            float px, float pz, Vector2 lightDir, Vector2[] polygon, float maxDistance)
        {
            // Ray: origin = (px, pz), direction = lightDir (toward the light).
            float rdx = lightDir.x;
            float rdz = lightDir.y;

            int count = polygon.Length;
            for (int i = 0; i < count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % count];

                float t = RaySegmentIntersection(px, pz, rdx, rdz, a.x, a.y, b.x, b.y);
                if (t >= 0f && t <= maxDistance)
                {
                    return true;
                }
            }

            return false;
        }

        // Tests if a caster polygon blocks the line of sight from a point to a light position.
        private static bool IsPointBlockedFromLight(
            float px, float pz, Vector2 lightPos, Vector2[] polygon)
        {
            float rdx = lightPos.x - px;
            float rdz = lightPos.y - pz;
            float lightDist = Mathf.Sqrt(rdx * rdx + rdz * rdz);
            if (lightDist < 0.001f) { return false; }

            // Normalize direction.
            rdx /= lightDist;
            rdz /= lightDist;

            int count = polygon.Length;
            for (int i = 0; i < count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % count];

                float t = RaySegmentIntersection(px, pz, rdx, rdz, a.x, a.y, b.x, b.y);
                if (t >= 0f && t < lightDist)
                {
                    return true;
                }
            }

            return false;
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

            // Parallel — no intersection.
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
