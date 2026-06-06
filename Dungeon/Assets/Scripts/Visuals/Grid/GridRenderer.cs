using UnityEngine;
using UnityEngine.Rendering;

namespace Dungeon.Visuals
{
    // Renders an infinite XZ grid in the game view using GL immediate-mode lines.
    // Uses RenderPipelineManager so it works correctly in URP.
    // Place on any GameObject. The grid plane sits at (transform.position.y + yLevel) in world space.
    public class GridRenderer : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        [Header("Grid Shape")]
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private float _yLevel = 0f;               // local Y offset from this transform
        [SerializeField] private Vector2 _xzOffset = Vector2.zero; // shifts where cell boundaries fall

        [Header("Render Budget")]
        [SerializeField] private int _renderRadius = 30;           // cells rendered around camera per axis

        [Header("Appearance")]
        [SerializeField] private Color _lineColor = new Color(1f, 1f, 1f, 0.5f);

        private Material _lineMaterial;
        private float WorldY => transform.position.y + _yLevel;

        private void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;

            CreateLineMaterial();
        }

        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null)
                Destroy(_lineMaterial);
        }

        private void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam != _camera || _lineMaterial == null) return;
            DrawGrid(cam);
        }

        private void DrawGrid(Camera cam)
        {
            _lineMaterial.SetPass(0);

            Vector3 camPos = cam.transform.position;

            // Floor-division so negative coords snap correctly
            int camCellX = Mathf.FloorToInt((camPos.x - _xzOffset.x) / _cellSize);
            int camCellZ = Mathf.FloorToInt((camPos.z - _xzOffset.y) / _cellSize);

            int minCellX = camCellX - _renderRadius;
            int maxCellX = camCellX + _renderRadius + 1; // +1 draws the far edge line
            int minCellZ = camCellZ - _renderRadius;
            int maxCellZ = camCellZ + _renderRadius + 1;

            float worldY    = WorldY;
            float worldMinX = minCellX * _cellSize + _xzOffset.x;
            float worldMaxX = maxCellX * _cellSize + _xzOffset.x;
            float worldMinZ = minCellZ * _cellSize + _xzOffset.y;
            float worldMaxZ = maxCellZ * _cellSize + _xzOffset.y;

            GL.PushMatrix();
            GL.LoadProjectionMatrix(cam.projectionMatrix);
            GL.modelview = cam.worldToCameraMatrix;
            GL.Begin(GL.LINES);
            GL.Color(_lineColor);

            // Lines running along Z (one per X boundary)
            for (int x = minCellX; x <= maxCellX; x++)
            {
                float wx = x * _cellSize + _xzOffset.x;
                GL.Vertex3(wx, worldY, worldMinZ);
                GL.Vertex3(wx, worldY, worldMaxZ);
            }

            // Lines running along X (one per Z boundary)
            for (int z = minCellZ; z <= maxCellZ; z++)
            {
                float wz = z * _cellSize + _xzOffset.y;
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
                Debug.LogError("[GridRenderer] Could not find 'Hidden/Internal-Colored' shader.");
                return;
            }

            _lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _lineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull",     (int)CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 0);
        }
    }
}
