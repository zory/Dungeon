using Dungeon.Logic.Services;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    public class LightingAuthoring : MonoBehaviour
    {
        [Header("Global Directional Light")]
        [Tooltip("When enabled, the global light illuminates everything. When disabled, everything is in darkness.")]
        [SerializeField] private bool _globalLightEnabled = true;

        [Tooltip("Direction angle in degrees (0 = +X, 90 = +Z). Defines where the light comes from.")]
        [SerializeField] [Range(0f, 360f)] private float _globalLightAngle = 225f;

        [Tooltip("Global light intensity. 0 = dark, 1 = fully lit with max shadow length.")]
        [SerializeField] [Range(0f, 1f)] private float _globalLightIntensity = 0.8f;

        public LightingConfig GetConfig() => new LightingConfig
        {
            GlobalLightEnabled = _globalLightEnabled,
            GlobalLightAngle = _globalLightAngle,
            GlobalLightIntensity = _globalLightIntensity,
        };

        private void Update()
        {
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null || bootstrapper.LogicWorld == null) { return; }
            if (!bootstrapper.LogicWorld.TryGet(out LightingService lighting)) { return; }

            lighting.GlobalLightEnabled = _globalLightEnabled;
            lighting.GlobalAngleDegrees = _globalLightAngle;
            lighting.GlobalIntensity = _globalLightIntensity;
        }
    }
}
