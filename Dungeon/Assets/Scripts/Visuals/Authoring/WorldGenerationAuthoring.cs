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

        public WorldGenerationConfig GetConfig() => new WorldGenerationConfig
        {
            Seed = _seed,
            RandomizeSeed = _randomizeSeed,
            NoiseScale = _noiseScale,
            Octaves = _octaves,
            Persistence = _persistence,
            Lacunarity = _lacunarity,
        };
    }
}
