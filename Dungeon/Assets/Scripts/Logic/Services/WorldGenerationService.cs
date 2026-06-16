using System;
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

        [Header("Terrain Thresholds (height 0-1)")]
        public float WaterLevel;
        public float BeachLevel;
        public float GrassLevel;
        public float DirtLevel;

        [Header("Tree Noise")]
        public float TreeNoiseScale;
        public float TreeThreshold;

        [Header("Underground")]
        public int DirtDepth;

        public static WorldGenerationConfig Default => new WorldGenerationConfig
        {
            Seed = 0,
            RandomizeSeed = false,
            NoiseScale = 0.025f,
            Octaves = 5,
            Persistence = 0.50f,
            Lacunarity = 2.00f,
            WaterLevel = 0.38f,
            BeachLevel = 0.43f,
            GrassLevel = 0.65f,
            DirtLevel = 0.80f,
            TreeNoiseScale = 0.08f,
            TreeThreshold = 0.62f,
            DirtDepth = 3,
        };
    }

    public class WorldGenerationService : ILogicService
    {
        private readonly WorldGenerationConfig _config;
        private readonly float _hx, _hz; // height noise offset
        private readonly float _mx, _mz; // tree / moisture noise offset

        public WorldGenerationService(WorldGenerationConfig config)
        {
            _config = config;

            int seed = config.RandomizeSeed ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : config.Seed;
            var rng = new System.Random(seed);
            _hx = (float)rng.NextDouble() * 99999f;
            _hz = (float)rng.NextDouble() * 99999f;
            _mx = (float)rng.NextDouble() * 99999f;
            _mz = (float)rng.NextDouble() * 99999f;
        }

        public void Initialize(LogicWorld world) { }

        public void Tick(float deltaTime) { }

        public int GetTileId(int x, int y, int z)
        {
            float h = FBm(x + _hx, z + _hz, _config.NoiseScale, _config.Octaves, _config.Persistence, _config.Lacunarity);

            if (y > 0)
                return (int)TileType.None;

            if (y == 0)
                return SurfaceTile(x, z, h);

            if (h < _config.WaterLevel)
                return (int)TileType.Water;

            return y >= -_config.DirtDepth ? (int)TileType.Dirt : (int)TileType.Rock;
        }

        private int SurfaceTile(int x, int z, float h)
        {
            if (h < _config.WaterLevel) return (int)TileType.Water;
            if (h < _config.BeachLevel) return (int)TileType.Dirt;
            if (h > _config.DirtLevel)  return (int)TileType.Rock;

            if (h < _config.GrassLevel)
            {
                float m = Mathf.PerlinNoise(x * _config.TreeNoiseScale + _mx, z * _config.TreeNoiseScale + _mz);
                return m > _config.TreeThreshold ? (int)TileType.Tree : (int)TileType.Grass;
            }

            return (int)TileType.Dirt;
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
