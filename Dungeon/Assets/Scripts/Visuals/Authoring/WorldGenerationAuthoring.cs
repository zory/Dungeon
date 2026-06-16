using Dungeon.Logic.Services;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    public class WorldGenerationAuthoring : MonoBehaviour
    {
        [Header("Seed")]
        [SerializeField] private int _seed = 0;
        [SerializeField] private bool _randomizeSeed = false;

        [Header("Height Noise")]
        [SerializeField] private float _noiseScale = 0.025f;
        [SerializeField] private int _octaves = 5;
        [SerializeField] private float _persistence = 0.50f;
        [SerializeField] private float _lacunarity = 2.00f;

        [Header("Terrain Thresholds (height 0-1)")]
        [SerializeField] private float _waterLevel = 0.38f;
        [SerializeField] private float _beachLevel = 0.43f;
        [SerializeField] private float _grassLevel = 0.65f;
        [SerializeField] private float _dirtLevel = 0.80f;

        [Header("Tree Noise")]
        [SerializeField] private float _treeNoiseScale = 0.08f;
        [SerializeField] private float _treeThreshold = 0.62f;

        [Header("Underground")]
        [SerializeField] private int _dirtDepth = 3;

        public WorldGenerationConfig GetConfig() => new WorldGenerationConfig
        {
            Seed = _seed,
            RandomizeSeed = _randomizeSeed,
            NoiseScale = _noiseScale,
            Octaves = _octaves,
            Persistence = _persistence,
            Lacunarity = _lacunarity,
            WaterLevel = _waterLevel,
            BeachLevel = _beachLevel,
            GrassLevel = _grassLevel,
            DirtLevel = _dirtLevel,
            TreeNoiseScale = _treeNoiseScale,
            TreeThreshold = _treeThreshold,
            DirtDepth = _dirtDepth,
        };
    }
}
