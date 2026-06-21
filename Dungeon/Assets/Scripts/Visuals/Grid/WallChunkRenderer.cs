using System.Collections.Generic;
using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Builds a single-pass mesh for one chunk of wall tiles using cardinal-neighbor autotiling.
    //
    // WALL TILES VS TERRAIN
    // ─────────────────────
    // Terrain uses dual-grid (corner bitmask NW/NE/SW/SE) because terrain is continuous.
    // Walls are discrete (wall or empty) so they use direct cardinal-neighbor autotiling:
    // each wall cell checks N/E/S/W for same-type neighbors → 4-bit bitmask → 16 tile variants.
    //
    // BITMASK BITS: N=1, E=2, S=4, W=8
    //
    // WALL QUAD GEOMETRY (3/4 perspective)
    // ────────────────────────────────────
    // Each wall cell produces a quad 1 cell wide (X) × 1.5 cells tall (Z).
    // The extra 0.5 cells extends south (toward the camera in top-down 3/4 view),
    // creating the front-face illusion. Quads are built north-to-south so that
    // southern walls overlap northern walls' front faces correctly.
    //
    // VERTEX DATA LAYOUT
    // ──────────────────
    // Uses the same packing as DualGridChunkRenderer (reuses DualGridTile shader):
    //   TEXCOORD0 — local quad UV [0,1]
    //   TEXCOORD1 — packed (wallTileIndex × 4 + 0) — single layer, rotation always 0
    //   TEXCOORD2 — x = 1 (single layer)
    //   TEXCOORD3 — packed RGB colour
    public class WallChunkRenderer : MonoBehaviour
    {
        public const int ChunkSize = WorldConstants.ChunkSize;

        private const int BIT_N = 1;
        private const int BIT_E = 2;
        private const int BIT_S = 4;
        private const int BIT_W = 8;

        private MeshFilter _filter;
        private MeshRenderer _renderer;

        private void Awake()
        {
            _filter = gameObject.AddComponent<MeshFilter>();
            _renderer = gameObject.AddComponent<MeshRenderer>();
        }

        private void OnDestroy()
        {
            if (_filter != null && _filter.sharedMesh != null)
            {
                Destroy(_filter.sharedMesh);
            }
        }

        /// <summary>
        /// Builds the wall mesh for this chunk.
        /// wallCells maps cell coord (Vector3Int) → obstacle type ID.
        /// </summary>
        public void Build(
            Vector2Int chunkCoord,
            int elevation,
            float cellSize,
            TerrainAtlas terrainAtlas,
            Material material,
            Dictionary<Vector3Int, int> wallCells)
        {
            // Push wall atlas configuration into the material.
            if (terrainAtlas.WallTexture != null)
            {
                material.SetTexture("_MainTex", terrainAtlas.WallTexture);
            }
            material.SetFloat("_AtlasColumns", terrainAtlas.WallColumns);
            material.SetFloat("_AtlasRows", terrainAtlas.WallRows);

            int baseX = chunkCoord.x * ChunkSize;
            int baseZ = chunkCoord.y * ChunkSize;

            int capacity = ChunkSize * ChunkSize * 4;

            List<Vector3> verts = new List<Vector3>(capacity);
            List<Vector2> uvs = new List<Vector2>(capacity);
            List<Vector4> terrainInfo = new List<Vector4>(capacity);
            List<Vector2> layerInfo = new List<Vector2>(capacity);
            List<Vector4> colorInfo = new List<Vector4>(capacity);
            List<int> tris = new List<int>(ChunkSize * ChunkSize * 6);

            float frontExtend = cellSize * 0.5f;

            // Build quads north-to-south (high Z first) for correct overlap.
            for (int j = ChunkSize - 1; j >= 0; j--)
            for (int i = 0; i < ChunkSize; i++)
            {
                int cx = baseX + i;
                int cz = baseZ + j;
                Vector3Int cellCoord = new Vector3Int(cx, elevation, cz);

                if (!wallCells.TryGetValue(cellCoord, out int typeId))
                {
                    continue;
                }

                // Compute cardinal bitmask: same-type neighbor → connected.
                int bitmask = 0;
                if (HasSameType(wallCells, cx, elevation, cz + 1, typeId)) { bitmask |= BIT_N; }
                if (HasSameType(wallCells, cx + 1, elevation, cz, typeId)) { bitmask |= BIT_E; }
                if (HasSameType(wallCells, cx, elevation, cz - 1, typeId)) { bitmask |= BIT_S; }
                if (HasSameType(wallCells, cx - 1, elevation, cz, typeId)) { bitmask |= BIT_W; }

                if (!terrainAtlas.GetWallTileInfo(typeId, bitmask, out int wallTileIndex))
                {
                    continue;
                }

                // Pack tile info (single layer, no rotation).
                float packed = wallTileIndex * 4f;
                float packedColor = PackColorRGB(terrainAtlas.GetColor(typeId));

                // Local quad coordinates within the chunk.
                // X: cell position, 1 cell wide.
                // Z: cell position + 0.5 cells south for front face (total 1.5 cells).
                float x0 = i * cellSize;
                float x1 = x0 + cellSize;
                float z0 = j * cellSize - frontExtend; // extends 0.5 south
                float z1 = z0 + cellSize + frontExtend; // total 1.5 cells tall

                AppendQuad(verts, uvs, terrainInfo, layerInfo, colorInfo, tris,
                           x0, x1, z0, z1,
                           new Vector4(packed, 0f, 0f, 0f),
                           new Vector4(packedColor, 0f, 0f, 0f),
                           1);
            }

            if (tris.Count == 0)
            {
                ClearMesh();
                return;
            }

            UploadMesh(verts, uvs, terrainInfo, layerInfo, colorInfo, tris);
            _renderer.sharedMaterial = material;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static bool HasSameType(Dictionary<Vector3Int, int> wallCells, int x, int y, int z, int typeId)
        {
            return wallCells.TryGetValue(new Vector3Int(x, y, z), out int neighborType) && neighborType == typeId;
        }

        private static float PackColorRGB(Color color)
        {
            float r = Mathf.Round(Mathf.Clamp01(color.r) * 255f);
            float g = Mathf.Round(Mathf.Clamp01(color.g) * 255f);
            float b = Mathf.Round(Mathf.Clamp01(color.b) * 255f);
            return r * 65536f + g * 256f + b;
        }

        private void ClearMesh()
        {
            if (_filter.sharedMesh != null)
            {
                _filter.sharedMesh.Clear();
            }
            _renderer.sharedMaterials = System.Array.Empty<Material>();
        }

        private static void AppendQuad(
            List<Vector3> verts, List<Vector2> uvs,
            List<Vector4> terrainInfos,
            List<Vector2> layerInfos,
            List<Vector4> colorInfos,
            List<int> tris,
            float x0, float x1, float z0, float z1,
            Vector4 ti, Vector4 ci, int layerCount)
        {
            int b = verts.Count;

            // Four corners on the XZ plane (Y = 0 in local chunk space).
            verts.Add(new Vector3(x0, 0f, z0)); // SW
            verts.Add(new Vector3(x1, 0f, z0)); // SE
            verts.Add(new Vector3(x0, 0f, z1)); // NW
            verts.Add(new Vector3(x1, 0f, z1)); // NE

            // Local quad UVs [0,1].
            uvs.Add(new Vector2(0f, 0f)); // SW
            uvs.Add(new Vector2(1f, 0f)); // SE
            uvs.Add(new Vector2(0f, 1f)); // NW
            uvs.Add(new Vector2(1f, 1f)); // NE

            // Per-quad data replicated to all 4 vertices.
            Vector2 li = new Vector2(layerCount, 0f);
            for (int k = 0; k < 4; k++)
            {
                terrainInfos.Add(ti);
                layerInfos.Add(li);
                colorInfos.Add(ci);
            }

            // CW winding viewed from above (+Y).
            tris.Add(b);     tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b + 1); tris.Add(b + 3); tris.Add(b + 2);
        }

        private void UploadMesh(
            List<Vector3> verts, List<Vector2> uvs,
            List<Vector4> terrainInfo,
            List<Vector2> layerInfo,
            List<Vector4> colorInfo,
            List<int> tris)
        {
            Mesh mesh = _filter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh { name = "WallChunkMesh" };
                _filter.sharedMesh = mesh;
            }

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, terrainInfo);
            mesh.SetUVs(2, layerInfo);
            mesh.SetUVs(3, colorInfo);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }
    }
}
