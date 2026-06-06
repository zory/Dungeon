using UnityEngine;

namespace Dungeon.Logic
{
    // Generates terrain using layered Perlin noise (fBm).
    // Returns TileType values cast to int — the TileRegistry in Visuals maps those to sprite-sheet indices.
    public class ProceduralWorldDataSource : WorldDataSource
    {
        [Header("Seed")]
        [SerializeField] private int  _seed          = 0;
        [SerializeField] private bool _randomizeSeed = false;

        [Header("Height Noise")]
        [SerializeField] private float _noiseScale  = 0.025f;
        [SerializeField] private int   _octaves     = 5;
        [SerializeField] private float _persistence = 0.50f;
        [SerializeField] private float _lacunarity  = 2.00f;

        [Header("Terrain Thresholds  (height 0–1)")]
        [SerializeField] private float _waterLevel = 0.38f;
        [SerializeField] private float _beachLevel = 0.43f;
        [SerializeField] private float _grassLevel = 0.65f;
        [SerializeField] private float _dirtLevel  = 0.80f;
        // above _dirtLevel → Rock

        [Header("Tree Noise")]
        [SerializeField] private float _treeNoiseScale = 0.08f;
        [SerializeField] private float _treeThreshold  = 0.62f;

        private float _hx, _hz;   // height noise offset
        private float _mx, _mz;   // tree / moisture noise offset

        private void Awake()
        {
            if (_randomizeSeed)
                _seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            var rng = new System.Random(_seed);
            _hx = NextFloat(rng) * 99999f;
            _hz = NextFloat(rng) * 99999f;
            _mx = NextFloat(rng) * 99999f;
            _mz = NextFloat(rng) * 99999f;
        }

        public override int GetTileId(int x, int y, int z)
        {
            float h = FBm(x + _hx, z + _hz, _noiseScale, _octaves, _persistence, _lacunarity);

            if (h < _waterLevel) return (int)TileType.Water;
            if (h < _beachLevel) return (int)TileType.Dirt;
            if (h > _dirtLevel)  return (int)TileType.Rock;

            if (h < _grassLevel)
            {
                float m = Mathf.PerlinNoise(x * _treeNoiseScale + _mx, z * _treeNoiseScale + _mz);
                return m > _treeThreshold ? (int)TileType.Tree : (int)TileType.Grass;
            }

            return (int)TileType.Dirt;
        }

        private static float FBm(float x, float z, float scale,
                                  int octaves, float persistence, float lacunarity)
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

        private static float NextFloat(System.Random rng) => (float)rng.NextDouble();
    }
}
