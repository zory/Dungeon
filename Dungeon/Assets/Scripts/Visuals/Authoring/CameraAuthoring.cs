using Dungeon.Visuals.Services;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    [RequireComponent(typeof(Camera))]
    public class CameraAuthoring : MonoBehaviour
    {
        [SerializeField] private float _minZoom = 1f;
        [SerializeField] private float _maxZoom = 10f;
        [SerializeField] private float _defaultZoom = 4f;
        [SerializeField] private float _zoomSpeed = 3f;

        public Camera Camera => GetComponent<Camera>();

        public CameraConfig GetConfig() => new CameraConfig
        {
            MinZoom = _minZoom,
            MaxZoom = _maxZoom,
            DefaultZoom = _defaultZoom,
            ZoomSpeed = _zoomSpeed,
        };
    }
}
