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

        [Tooltip("Sun elevation above the horizon in degrees. 0 = horizon (very long shadows), 90 = directly overhead (no shadows).")]
        [SerializeField] [Range(0f, 90f)] private float _globalElevationAngle = 45f;

        [Tooltip("Maximum shadow distance cap to prevent infinitely long shadows at low sun angles.")]
        [SerializeField] [Min(1f)] private float _maxShadowDistance = 50f;

        public LightingConfig GetConfig() => new LightingConfig
        {
            GlobalLightEnabled = _globalLightEnabled,
            GlobalLightAngle = _globalLightAngle,
            GlobalElevationAngle = _globalElevationAngle,
            MaxShadowDistance = _maxShadowDistance,
        };

        private void Update()
        {
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null || bootstrapper.LogicWorld == null) { return; }
            if (!bootstrapper.LogicWorld.TryGet(out LightingService lighting)) { return; }

            lighting.GlobalLightEnabled = _globalLightEnabled;
            lighting.GlobalAngleDegrees = _globalLightAngle;
            lighting.GlobalElevationAngle = _globalElevationAngle;
        }
    }
}
