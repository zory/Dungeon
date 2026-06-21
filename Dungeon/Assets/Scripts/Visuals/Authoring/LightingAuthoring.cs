using Dungeon.Logic.Services;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    public class LightingAuthoring : MonoBehaviour
    {
        [Header("Global Directional Light")]
        [Tooltip("Direction angle in degrees (0 = +X, 90 = +Z). Defines where the light comes from.")]
        [SerializeField] [Range(0f, 360f)] private float _globalLightAngle = 225f;

        [Tooltip("Global light intensity. 0 = dark, 1 = fully lit with max shadow length.")]
        [SerializeField] [Range(0f, 1f)] private float _globalLightIntensity = 0.8f;

        public LightingConfig GetConfig() => new LightingConfig
        {
            GlobalLightAngle = _globalLightAngle,
            GlobalLightIntensity = _globalLightIntensity,
        };
    }
}
