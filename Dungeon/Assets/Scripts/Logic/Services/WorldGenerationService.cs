using System;
using System.Collections.Generic;
using Dungeon.Logic.Core;
using UnityEngine;

namespace Dungeon.Logic.Services
{
    [Serializable]
    public struct WorldGenerationConfig
    {
        public int Seed;
        public bool RandomizeSeed;

        [Header("Height Noise")]
        public float NoiseScale;
        public int Octaves;
        public float Persistence;
        public float Lacunarity;

        public static WorldGenerationConfig Default => new WorldGenerationConfig
        {
            Seed = 0,
            RandomizeSeed = false,
            NoiseScale = 0.025f,
            Octaves = 5,
            Persistence = 0.50f,
            Lacunarity = 2.00f,
        };
    }

    public class WorldGenerationService : ILogicService
    {
        private readonly WorldGenerationConfig _config;
        private readonly float _hx, _hz; // height noise offset
        private readonly float _mx, _mz; // secondary noise offset

        // Pre-sorted rule sets for fast lookup.
        private readonly TerrainGenerationRule[] _surfaceRules;     // sorted by SurfaceHeightMin ascending
        private readonly TerrainGenerationRule[] _undergroundRules; // sorted by UndergroundDepthMin ascending
        private readonly TerrainGenerationRule[] _extendsRules;     // rules with ExtendsUnderground == true

        // The effective seed used for generation (after randomisation).
        public int Seed { get; private set; }

        public WorldGenerationService(WorldGenerationConfig config, TerrainGenerationRule[] rules)
        {
            _config = config;

            int seed = config.RandomizeSeed ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : config.Seed;
            Seed = seed;
            var rng = new System.Random(seed);
            _hx = (float)rng.NextDouble() * 99999f;
            _hz = (float)rng.NextDouble() * 99999f;
            _mx = (float)rng.NextDouble() * 99999f;
            _mz = (float)rng.NextDouble() * 99999f;

            // Partition rules into surface, underground, and extends-underground sets.
            var surface = new List<TerrainGenerationRule>();
            var underground = new List<TerrainGenerationRule>();
            var extends_ = new List<TerrainGenerationRule>();

            foreach (TerrainGenerationRule rule in rules)
            {
                if (rule.SurfaceHeightMin >= 0f && rule.SurfaceHeightMax > rule.SurfaceHeightMin)
                {
                    surface.Add(rule);
                }
                if (rule.UndergroundDepthMin > 0 && rule.UndergroundDepthMax >= rule.UndergroundDepthMin)
                {
                    underground.Add(rule);
                }
                if (rule.ExtendsUnderground)
                {
                    extends_.Add(rule);
                }
            }

            surface.Sort((a, b) => a.SurfaceHeightMin.CompareTo(b.SurfaceHeightMin));
            underground.Sort((a, b) => a.UndergroundDepthMin.CompareTo(b.UndergroundDepthMin));

            _surfaceRules = surface.ToArray();
            _undergroundRules = underground.ToArray();
            _extendsRules = extends_.ToArray();
        }

        public void Initialize(LogicWorld world) { }

        public void Tick(float deltaTime) { }

        public int GetTileId(int x, int y, int z)
        {
            if (y > 0)
            {
                return 0;
            }

            float h = FBm(x + _hx, z + _hz, _config.NoiseScale, _config.Octaves, _config.Persistence, _config.Lacunarity);

            if (y == 0)
            {
                return SurfaceTile(x, z, h);
            }

            // Underground: check if the surface type extends underground.
            int surfaceTileId = SurfaceTileRaw(h);
            foreach (TerrainGenerationRule rule in _extendsRules)
            {
                if (rule.TileId == surfaceTileId)
                {
                    return rule.TileId;
                }
            }

            // Otherwise, find the underground rule by depth.
            int depth = -y; // y=-1 => depth=1, y=-2 => depth=2, etc.
            return UndergroundTile(depth);
        }

        private int SurfaceTile(int x, int z, float h)
        {
            int tileId = SurfaceTileRaw(h);

            // Check for secondary noise overlay on the matched rule.
            foreach (TerrainGenerationRule rule in _surfaceRules)
            {
                if (rule.TileId == tileId && rule.SecondaryNoiseScale > 0f)
                {
                    float m = Mathf.PerlinNoise(x * rule.SecondaryNoiseScale + _mx, z * rule.SecondaryNoiseScale + _mz);
                    if (m > rule.SecondaryNoiseThreshold)
                    {
                        return rule.SecondaryTileId;
                    }
                    break;
                }
            }

            return tileId;
        }

        private int SurfaceTileRaw(float h)
        {
            // Walk surface rules in ascending HeightMin order.
            // The first rule whose band contains h wins.
            foreach (TerrainGenerationRule rule in _surfaceRules)
            {
                if (h >= rule.SurfaceHeightMin && h < rule.SurfaceHeightMax)
                {
                    return rule.TileId;
                }
            }

            // Fallback: last surface rule covers everything above its max.
            if (_surfaceRules.Length > 0)
            {
                return _surfaceRules[_surfaceRules.Length - 1].TileId;
            }

            return 0;
        }

        private int UndergroundTile(int depth)
        {
            foreach (TerrainGenerationRule rule in _undergroundRules)
            {
                if (depth >= rule.UndergroundDepthMin && depth <= rule.UndergroundDepthMax)
                {
                    return rule.TileId;
                }
            }

            // Fallback: deepest rule covers everything below.
            if (_undergroundRules.Length > 0)
            {
                return _undergroundRules[_undergroundRules.Length - 1].TileId;
            }

            return 0;
        }

        private static float FBm(float x, float z, float scale, int octaves, float persistence, float lacunarity)
        {
            float value = 0f, amplitude = 1f, frequency = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                value     += Mathf.PerlinNoise(x * scale * frequency, z * scale * frequency) * amplitude;
                norm      += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            return value / norm;
        }

        public void Dispose() { }
    }
}
