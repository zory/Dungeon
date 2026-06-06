using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Builds and owns a single flat mesh covering ChunkSize x ChunkSize cells on the XZ plane.
    // TileType.None cells are skipped (no quad generated).
    // All other types are resolved through TileRegistry to a sprite-sheet index, then UV-mapped.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ChunkRenderer : MonoBehaviour
    {
        public const int ChunkSize = Dungeon.Logic.WorldConstants.ChunkSize;

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

        public void Build(Vector2Int chunkCoord, GridManager gridManager,
                          GridRenderer gridRenderer, TileRegistry registry,
                          int sheetColumns, int sheetRows)
        {
            var   grid      = gridManager.Grid;
            float cs        = gridRenderer.CellSize;
            int   elevation = gridRenderer.ElevationLayer;
            int   baseX     = chunkCoord.x * ChunkSize;
            int   baseZ     = chunkCoord.y * ChunkSize;

            float invCols = 1f / sheetColumns;
            float invRows = 1f / sheetRows;

            // Pass 1 — count renderable cells so we allocate exactly the right arrays
            int renderCount = 0;
            for (int lz = 0; lz < ChunkSize; lz++)
            for (int lx = 0; lx < ChunkSize; lx++)
            {
                int t = grid.GetCell(new Vector3Int(baseX + lx, elevation, baseZ + lz))?.TileId ?? 0;
                if (!registry.IsNone(t)) renderCount++;
            }

            var vertices = new Vector3[renderCount * 4];
            var uvs      = new Vector2[renderCount * 4];
            var indices  = new int[renderCount * 6];

            int vi = 0, ii = 0;

            // Pass 2 — fill geometry
            for (int lz = 0; lz < ChunkSize; lz++)
            for (int lx = 0; lx < ChunkSize; lx++)
            {
                int tileTypeId = grid.GetCell(new Vector3Int(baseX + lx, elevation, baseZ + lz))?.TileId ?? 0;
                if (registry.IsNone(tileTypeId)) continue;

                int sheetIdx = registry.GetSheetIndex(tileTypeId);
                int col = sheetIdx % sheetColumns;
                int row = sheetIdx / sheetColumns;

                float uMin = col       * invCols;
                float uMax = (col + 1) * invCols;
                float vMax = 1f - row       * invRows;   // row 0 = top of sheet, V flipped
                float vMin = 1f - (row + 1) * invRows;

                float x0 = lx * cs, x1 = x0 + cs;
                float z0 = lz * cs, z1 = z0 + cs;

                vertices[vi + 0] = new Vector3(x0, 0f, z0);
                vertices[vi + 1] = new Vector3(x0, 0f, z1);
                vertices[vi + 2] = new Vector3(x1, 0f, z1);
                vertices[vi + 3] = new Vector3(x1, 0f, z0);

                uvs[vi + 0] = new Vector2(uMin, vMin);
                uvs[vi + 1] = new Vector2(uMin, vMax);
                uvs[vi + 2] = new Vector2(uMax, vMax);
                uvs[vi + 3] = new Vector2(uMax, vMin);

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
