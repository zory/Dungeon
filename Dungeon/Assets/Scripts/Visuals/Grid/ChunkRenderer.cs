using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Builds and owns a single flat mesh covering ChunkSize x ChunkSize cells on the XZ plane.
    // Each cell is a quad UV-mapped into the sprite sheet based on its TileId.
    // Created and managed by WorldRenderer — do not add to scene manually.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ChunkRenderer : MonoBehaviour
    {
        public const int ChunkSize = 64;

        private Mesh _mesh;

        private void Awake()
        {
            _mesh = new Mesh { name = "ChunkMesh" };
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            GetComponent<MeshFilter>().mesh = _mesh;
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }

        // sheetColumns / sheetRows describe the sprite sheet grid.
        // TileId 0 = top-left cell, numbered left-to-right then top-to-bottom.
        public void Build(Vector2Int chunkCoord, GridManager gridManager,
                          GridRenderer gridRenderer, int sheetColumns, int sheetRows)
        {
            var   grid      = gridManager.Grid;
            float cs        = gridRenderer.CellSize;
            int   elevation = gridRenderer.ElevationLayer;
            int   baseX     = chunkCoord.x * ChunkSize;
            int   baseZ     = chunkCoord.y * ChunkSize;

            float invCols = 1f / sheetColumns;
            float invRows = 1f / sheetRows;

            const int cellCount = ChunkSize * ChunkSize;
            var vertices = new Vector3[cellCount * 4];
            var uvs      = new Vector2[cellCount * 4];
            var indices  = new int[cellCount * 6];

            int vi = 0, ii = 0;

            for (int lz = 0; lz < ChunkSize; lz++)
            for (int lx = 0; lx < ChunkSize; lx++)
            {
                int tileId = grid.GetCell(new Vector3Int(baseX + lx, elevation, baseZ + lz))?.TileId ?? 0;

                int col = tileId % sheetColumns;
                int row = tileId / sheetColumns;

                float uMin = col       * invCols;
                float uMax = (col + 1) * invCols;
                // Row 0 is top of the sheet; UV V=0 is bottom → flip
                float vMax = 1f - row       * invRows;
                float vMin = 1f - (row + 1) * invRows;

                float x0 = lx * cs, x1 = x0 + cs;
                float z0 = lz * cs, z1 = z0 + cs;

                // Quad flat on XZ (y = 0 in chunk local space)
                vertices[vi + 0] = new Vector3(x0, 0f, z0);
                vertices[vi + 1] = new Vector3(x0, 0f, z1);
                vertices[vi + 2] = new Vector3(x1, 0f, z1);
                vertices[vi + 3] = new Vector3(x1, 0f, z0);

                uvs[vi + 0] = new Vector2(uMin, vMin);
                uvs[vi + 1] = new Vector2(uMin, vMax);
                uvs[vi + 2] = new Vector2(uMax, vMax);
                uvs[vi + 3] = new Vector2(uMax, vMin);

                // CCW winding viewed from +Y → normal points up toward camera
                indices[ii + 0] = vi;     indices[ii + 1] = vi + 1; indices[ii + 2] = vi + 2;
                indices[ii + 3] = vi;     indices[ii + 4] = vi + 2; indices[ii + 5] = vi + 3;

                vi += 4;
                ii += 6;
            }

            _mesh.Clear();
            _mesh.vertices  = vertices;
            _mesh.uv        = uvs;
            _mesh.triangles = indices;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }
    }
}
