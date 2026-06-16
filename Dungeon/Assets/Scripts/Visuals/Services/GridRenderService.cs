using System;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dungeon.Visuals.Services
{
    [Serializable]
    public struct GridRenderConfig
    {
        public int RenderRadius;
        public Color LineColor;

        public static GridRenderConfig Default => new GridRenderConfig
        {
            RenderRadius = 30,
            LineColor = new Color(1f, 1f, 1f, 0.5f),
        };
    }

    public class GridRenderService : IVisualService
    {
        private readonly GridRenderConfig _config;
        private VisualWorld _world;
        private GridService _grid;
        private CameraService _camera;
        private Material _lineMaterial;

        public GridRenderService(GridRenderConfig config)
        {
            _config = config;
        }

        public void Initialize(VisualWorld world)
        {
            _world = world;
            _grid = world.GetLogic<GridService>();
            _camera = world.Get<CameraService>();
            CreateLineMaterial();
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        public void Tick(float deltaTime) { }

        private void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam != _camera.Camera || _lineMaterial == null) return;
            DrawGrid(cam);
        }

        private void DrawGrid(Camera cam)
        {
            _lineMaterial.SetPass(0);

            float cellSize = _grid.CellSize;
            Vector2 xzOffset = _grid.XZOffset;
            float worldY = _grid.WorldY;

            Vector3 camPos = cam.transform.position;
            int camCellX = Mathf.FloorToInt((camPos.x - xzOffset.x) / cellSize);
            int camCellZ = Mathf.FloorToInt((camPos.z - xzOffset.y) / cellSize);

            int minCellX = camCellX - _config.RenderRadius;
            int maxCellX = camCellX + _config.RenderRadius + 1;
            int minCellZ = camCellZ - _config.RenderRadius;
            int maxCellZ = camCellZ + _config.RenderRadius + 1;

            float worldMinX = minCellX * cellSize + xzOffset.x;
            float worldMaxX = maxCellX * cellSize + xzOffset.x;
            float worldMinZ = minCellZ * cellSize + xzOffset.y;
            float worldMaxZ = maxCellZ * cellSize + xzOffset.y;

            GL.PushMatrix();
            GL.LoadProjectionMatrix(cam.projectionMatrix);
            GL.modelview = cam.worldToCameraMatrix;
            GL.Begin(GL.LINES);
            GL.Color(_config.LineColor);

            for (int x = minCellX; x <= maxCellX; x++)
            {
                float wx = x * cellSize + xzOffset.x;
                GL.Vertex3(wx, worldY, worldMinZ);
                GL.Vertex3(wx, worldY, worldMaxZ);
            }

            for (int z = minCellZ; z <= maxCellZ; z++)
            {
                float wz = z * cellSize + xzOffset.y;
                GL.Vertex3(worldMinX, worldY, wz);
                GL.Vertex3(worldMaxX, worldY, wz);
            }

            GL.End();
            GL.PopMatrix();
        }

        private void CreateLineMaterial()
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                Debug.LogError("[GridRenderService] Could not find 'Hidden/Internal-Colored' shader.");
                return;
            }

            _lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _lineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull",     (int)CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 0);
        }

        public void Dispose()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            if (_lineMaterial != null)
                UnityEngine.Object.Destroy(_lineMaterial);
        }
    }
}
