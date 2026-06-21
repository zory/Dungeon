using System.Collections.Generic;
using Dungeon.Logic;
using Dungeon.Logic.Services;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Builds a single-pass mesh for one chunk using dual-grid autotiling.
    //
    // SINGLE-PASS RENDERING
    // ─────────────────────
    // Each visual tile is one quad.  Up to four terrain layers are packed into
    // vertex attributes.  The fragment shader composites layers bottom-to-top,
    // sampling full RGBA terrain tiles from the atlas, tinted by per-type
    // colour — all in one draw call per chunk.
    //
    // VERTEX DATA LAYOUT
    // ──────────────────
    //   POSITION   (float3)  — quad corner in local chunk space (Y = 0)
    //   TEXCOORD0  (float2)  — local quad UV [0,1]
    //   TEXCOORD1  (float4)  — packed (terrainTileIndex × 4 + rotation) per layer 0-3
    //                          (rotation is always 0 — each bitmask has its own tile)
    //   TEXCOORD2  (float2)  — x = active layer count
    //   TEXCOORD3  (float4)  — packed RGB colour per layer 0-3
    //                          (R×65536 + G×256 + B, each channel 0-255)
    //
    // DUAL-GRID + HALF-CELL OFFSET
    // ────────────────────────────
    // Each visual tile covers the same world area as a data cell but samples
    // four surrounding data cells to compute a 4-bit bitmask per terrain type.
    // The visual grid is shifted +0.5 cells in X and Z to centre each visual
    // tile on the junction of the 4 sampled data cells.
    //
    //   Visual tile (i, j) samples:
    //     SW = cell (baseX + i,     y, baseZ + j    )
    //     SE = cell (baseX + i + 1, y, baseZ + j    )
    //     NW = cell (baseX + i,     y, baseZ + j + 1)
    //     NE = cell (baseX + i + 1, y, baseZ + j + 1)
    //   Bitmask bits: NW=1, NE=2, SW=4, SE=8
    //
    // ASYMMETRIC TILES — NO UV ROTATION
    // ──────────────────────────────────
    // Each terrain type provides 16 pre-painted tiles (one per bitmask 0–15).
    // The bitmask indexes directly into the terrain's tile range.  No UV
    // rotation is applied — tiles are painted exactly as they appear on screen.
    //
    // HIDDEN TYPES
    // ────────────
    // Types not registered in TerrainAtlas (or with negative priority) are
    // hidden.  Hidden corners extend coverage for whichever visible terrain
    // types appear at the tile, but the type must be present in at least one
    // non-hidden corner.
    public class DualGridChunkRenderer : MonoBehaviour
    {
        public const int ChunkSize = WorldConstants.ChunkSize;

        private const int BIT_NW = 1;
        private const int BIT_NE = 2;
        private const int BIT_SW = 4;
        private const int BIT_SE = 8;

        private MeshFilter   _filter;
        private MeshRenderer _renderer;

        private void Awake()
        {
            _filter   = gameObject.AddComponent<MeshFilter>();
            _renderer = gameObject.AddComponent<MeshRenderer>();
        }

        private void OnDestroy()
        {
            if (_filter != null && _filter.sharedMesh != null)
            {
                Destroy(_filter.sharedMesh);
            }
        }

        public void Build(
            Vector2Int         chunkCoord,
            CellGrid           grid,
            int                elevation,
            float              cellSize,
            TerrainAtlas       terrainAtlas,
            Material           material,
            UndergroundService underground = null)
        {
            // Push atlas configuration into the shared material.
            if (terrainAtlas.Texture != null)
            {
                material.SetTexture("_MainTex", terrainAtlas.Texture);
            }
            material.SetFloat("_AtlasColumns", terrainAtlas.Columns);
            material.SetFloat("_AtlasRows", terrainAtlas.Rows);

            int baseX = chunkCoord.x * ChunkSize;
            int baseZ = chunkCoord.y * ChunkSize;

            int capacity = ChunkSize * ChunkSize * 4;

            List<Vector3> verts       = new List<Vector3>(capacity);
            List<Vector2> uvs         = new List<Vector2>(capacity);
            List<Vector4> terrainInfo = new List<Vector4>(capacity);
            List<Vector2> layerInfo   = new List<Vector2>(capacity);
            List<Vector4> colorInfo   = new List<Vector4>(capacity);
            List<int>     tris        = new List<int>(ChunkSize * ChunkSize * 6);

            // Reusable per-tile buffers (max 4 terrain types per tile).
            List<int> tileTypes = new List<int>(4);
            List<int> tileMasks = new List<int>(4);
            List<int> tilePris  = new List<int>(4);

            float halfCell = cellSize * 0.5f;
            bool isUnderground = UndergroundService.IsUnderground(elevation);

            for (int j = 0; j < ChunkSize; j++)
            for (int i = 0; i < ChunkSize; i++)
            {
                int gx = baseX + i;
                int gz = baseZ + j;

                int swType = ResolveType(grid, terrainAtlas, underground, isUnderground, gx,     elevation, gz);
                int seType = ResolveType(grid, terrainAtlas, underground, isUnderground, gx + 1, elevation, gz);
                int nwType = ResolveType(grid, terrainAtlas, underground, isUnderground, gx,     elevation, gz + 1);
                int neType = ResolveType(grid, terrainAtlas, underground, isUnderground, gx + 1, elevation, gz + 1);

                // Which corners are hidden (not rendered directly).
                int hiddenMask = 0;
                if (IsHidden(terrainAtlas, nwType)) { hiddenMask |= BIT_NW; }
                if (IsHidden(terrainAtlas, neType)) { hiddenMask |= BIT_NE; }
                if (IsHidden(terrainAtlas, swType)) { hiddenMask |= BIT_SW; }
                if (IsHidden(terrainAtlas, seType)) { hiddenMask |= BIT_SE; }

                // Collect unique visible terrain types and their combined bitmasks.
                tileTypes.Clear();
                tileMasks.Clear();
                tilePris.Clear();

                AddCornerType(tileTypes, tileMasks, tilePris, terrainAtlas, nwType, BIT_NW, hiddenMask);
                AddCornerType(tileTypes, tileMasks, tilePris, terrainAtlas, neType, BIT_NE, hiddenMask);
                AddCornerType(tileTypes, tileMasks, tilePris, terrainAtlas, swType, BIT_SW, hiddenMask);
                AddCornerType(tileTypes, tileMasks, tilePris, terrainAtlas, seType, BIT_SE, hiddenMask);

                if (tileTypes.Count == 0) { continue; }

                // Sort by ascending priority (lowest = bottom layer = rendered first).
                SortByPriority(tileTypes, tileMasks, tilePris);

                // Pack per-layer data.
                int layerCount = tileTypes.Count > 4 ? 4 : tileTypes.Count;
                Vector4 ti = Vector4.zero;
                Vector4 ci = Vector4.zero;

                for (int l = 0; l < layerCount; l++)
                {
                    if (!terrainAtlas.GetTileInfo(tileTypes[l], tileMasks[l],
                            out int terrainTile, out int rot))
                    {
                        continue;
                    }
                    float packed = terrainTile * 4f + rot;
                    float packedColor = PackColorRGB(terrainAtlas.GetColor(tileTypes[l]));

                    switch (l)
                    {
                        case 0: ti.x = packed; ci.x = packedColor; break;
                        case 1: ti.y = packed; ci.y = packedColor; break;
                        case 2: ti.z = packed; ci.z = packedColor; break;
                        case 3: ti.w = packed; ci.w = packedColor; break;
                    }
                }

                // +halfCell offset centres the visual tile on the junction of
                // the 4 sampled data cells (dual-grid half-cell shift).
                float x0 = i * cellSize + halfCell;
                float x1 = x0 + cellSize;
                float z0 = j * cellSize + halfCell;
                float z1 = z0 + cellSize;

                AppendQuad(verts, uvs, terrainInfo, layerInfo, colorInfo, tris,
                           x0, x1, z0, z1, ti, ci, layerCount);
            }

            if (tris.Count == 0)
            {
                ClearMesh();
                return;
            }

            UploadMesh(verts, uvs, terrainInfo, layerInfo, colorInfo, tris);

            // Single material — one draw call per chunk.
            _renderer.sharedMaterial = material;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        // Packs an RGB colour into a single float: R×65536 + G×256 + B
        // (each channel quantised to 0–255).  Fits exactly within float's
        // 24-bit mantissa — no precision loss for integer values up to 2^24.
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

        // Returns the tile ID for rendering at a given cell position.
        // For underground unrevealed cells, substitutes NOT_REVEALED_ID so the
        // renderer draws fog-of-war instead of the actual terrain.
        private static int ResolveType(CellGrid grid, TerrainAtlas atlas,
                                        UndergroundService underground, bool isUnderground,
                                        int x, int y, int z)
        {
            Cell cell = grid.GetCell(new Vector3Int(x, y, z));
            if (cell == null) { return atlas.NotInitializedId; }

            int tileId = cell.TileId;

            // Unregistered tiles stay as-is (hidden by IsHidden).
            if (atlas.GetPriority(tileId) < 0)
            {
                return tileId;
            }

            // Underground cells that are not revealed render as fog-of-war.
            if (isUnderground && underground != null && !underground.IsRevealed(new Vector3Int(x, y, z)))
            {
                return TerrainAtlas.NOT_REVEALED_ID;
            }

            return tileId;
        }

        private static bool IsHidden(TerrainAtlas atlas, int t)
        {
            if (t == atlas.NotInitializedId) { return true; }
            if (t == TerrainAtlas.NOT_REVEALED_ID) { return false; }
            return atlas.GetPriority(t) < 0;
        }

        // Adds a corner's terrain type to the per-tile collection.  If the type
        // is already present, merges the corner bit into its mask.  Hidden types
        // are skipped, but their bits extend all present types via hiddenMask.
        private static void AddCornerType(
            List<int> types, List<int> masks, List<int> priorities,
            TerrainAtlas atlas, int cornerType, int cornerBit, int hiddenMask)
        {
            if (IsHidden(atlas, cornerType)) { return; }

            int idx = types.IndexOf(cornerType);
            if (idx >= 0)
            {
                masks[idx] = masks[idx] | cornerBit;
            }
            else
            {
                types.Add(cornerType);
                masks.Add(cornerBit | hiddenMask);
                priorities.Add(atlas.GetPriority(cornerType));
            }
        }

        // Insertion sort by priority (ascending).  At most 4 elements.
        private static void SortByPriority(List<int> types, List<int> masks, List<int> priorities)
        {
            for (int a = 1; a < types.Count; a++)
            {
                int tA = types[a];
                int mA = masks[a];
                int pA = priorities[a];
                int b = a - 1;
                while (b >= 0 && priorities[b] > pA)
                {
                    types[b + 1]      = types[b];
                    masks[b + 1]      = masks[b];
                    priorities[b + 1] = priorities[b];
                    b--;
                }
                types[b + 1]      = tA;
                masks[b + 1]      = mA;
                priorities[b + 1] = pA;
            }
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

            // Local quad UVs [0,1] — the shader handles atlas lookup and rotation.
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
                mesh = new Mesh { name = "ChunkMesh" };
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
