using System.Collections.Generic;
using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Builds a single flat mesh for a chunk using dual-grid autotiling.
    //
    // DUAL-GRID OVERVIEW
    // ------------------
    // The data grid stores one TileType per cell (x, y, z).
    // Each visual tile covers the same world area as a data cell but looks at FOUR surrounding
    // data cells to compute a 4-bit bitmask that selects a shape from the DualGridAtlas.
    //
    //   Visual tile at local (i, j) samples:
    //     SW = cell (baseX + i,     y, baseZ + j    )
    //     SE = cell (baseX + i + 1, y, baseZ + j    )
    //     NW = cell (baseX + i,     y, baseZ + j + 1)
    //     NE = cell (baseX + i + 1, y, baseZ + j + 1)
    //
    // Bitmask bits:  NW=1, NE=2, SW=4, SE=8
    //
    // SINGLE-PASS RENDERING
    // ---------------------
    // One mesh, one material (DualGridTile shader).
    // Per-vertex COLOR     = primary tint  (atlas-opaque areas)
    // Per-vertex TEXCOORD1 = secondary tint (atlas-transparent areas)
    // The shader blends both in a single pass using the atlas alpha.
    //
    // HIDDEN TYPES
    // ------------
    // Types not registered in TileColorRegistry (e.g. Tree, Darkness) are "hidden."
    // Hidden cells are transparent to the renderer: they match whichever primary type
    // their visible neighbours have, so they never create visual boundaries.
    //
    // TYPE LIMIT PER TILE
    // -------------------
    // Each tile can only show TWO types: primary (highest priority) and secondary
    // (second-highest priority). When 3+ types share a corner, the 3rd/4th types
    // are visually merged into the secondary colour. This is a fundamental limitation
    // of the 4-bit / 8-shape dual-grid approach.
    //
    // BITMASK → TILE MAPPING
    // ──────────────────────
    // The DualGridAtlas stores which bitmask each of its 8 tiles represents.
    // For any computed bitmask (0–15) the atlas returns the correct UV rect
    // and a swap flag (true when using the complement tile).
    public class DualGridChunkRenderer : MonoBehaviour
    {
        public const int ChunkSize = WorldConstants.ChunkSize;

        private const int BIT_NW = 1 << 0; // 1
        private const int BIT_NE = 1 << 1; // 2
        private const int BIT_SW = 1 << 2; // 4
        private const int BIT_SE = 1 << 3; // 8

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
                Destroy(_filter.sharedMesh);
        }

        public void Build(
            Vector2Int        chunkCoord,
            GridManager       gridManager,
            GridRenderer      gridRenderer,
            DualGridAtlas     atlas,
            TileColorRegistry colorRegistry,
            Material          material)
        {
            _renderer.sharedMaterial = material;

            if (atlas.Atlas != null)
                material.mainTexture = atlas.Atlas;

            int       elevation = gridRenderer.ElevationLayer;
            float     cs        = gridRenderer.CellSize;
            CellGrid  grid      = gridManager.Grid;
            int       baseX     = chunkCoord.x * ChunkSize;
            int       baseZ     = chunkCoord.y * ChunkSize;

            var verts      = new List<Vector3>(ChunkSize * ChunkSize * 4);
            var uvs        = new List<Vector2>(ChunkSize * ChunkSize * 4);
            var primColors = new List<Color>  (ChunkSize * ChunkSize * 4);
            var secColors  = new List<Vector4>(ChunkSize * ChunkSize * 4);
            var tris       = new List<int>    (ChunkSize * ChunkSize * 6);

            for (int j = 0; j < ChunkSize; j++)
            for (int i = 0; i < ChunkSize; i++)
            {
                int gx = baseX + i;
                int gz = baseZ + j;

                int swType = CellType(grid, gx,     elevation, gz    );
                int seType = CellType(grid, gx + 1, elevation, gz    );
                int nwType = CellType(grid, gx,     elevation, gz + 1);
                int neType = CellType(grid, gx + 1, elevation, gz + 1);

                if (AllNone(colorRegistry, swType, seType, nwType, neType)) continue;

                // Primary = highest-priority visible type among the 4 cells.
                int primaryType = HighestPriority(colorRegistry, swType, seType, nwType, neType, exclude: -1);

                // Bitmask: visible cells matching primary set their bit.
                // Hidden cells (unregistered types, None, unloaded chunks) also set
                // their bit so they never create a visual boundary.
                int mask = 0;
                if (IsHidden(colorRegistry, nwType) || nwType == primaryType) mask |= BIT_NW;
                if (IsHidden(colorRegistry, neType) || neType == primaryType) mask |= BIT_NE;
                if (IsHidden(colorRegistry, swType) || swType == primaryType) mask |= BIT_SW;
                if (IsHidden(colorRegistry, seType) || seType == primaryType) mask |= BIT_SE;

                // Atlas resolves the bitmask to a tile UV and whether to swap colours.
                Rect uvRect = atlas.GetUVRect(mask, out bool swapColors);

                // Secondary = highest-priority visible type that differs from primary.
                int secondaryType = HighestPriority(colorRegistry, swType, seType, nwType, neType,
                                                    exclude: primaryType);

                Color primaryColor   = colorRegistry.GetColor(primaryType);

                // When there is no real secondary type, fill with primary so the
                // tile is solid — avoids transparent gaps and dark-fringe artifacts.
                Color secondaryColor = IsHidden(colorRegistry, secondaryType)
                    ? primaryColor
                    : colorRegistry.GetColor(secondaryType);

                // When swapColors, atlas shape represents secondary cells → swap assignments.
                Color vertPrimary   = swapColors ? secondaryColor : primaryColor;
                Color vertSecondary = swapColors ? primaryColor   : secondaryColor;

                float x0 = i * cs, x1 = x0 + cs;
                float z0 = j * cs, z1 = z0 + cs;

                AppendQuad(verts, uvs, primColors, secColors, tris,
                           x0, x1, z0, z1, uvRect, vertPrimary, vertSecondary);
            }

            UploadMesh(_filter, verts, uvs, primColors, secColors, tris);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static int CellType(CellGrid grid, int x, int y, int z)
            => grid.GetCell(new Vector3Int(x, y, z))?.TileId ?? (int)TileType.None;

        // A type is hidden if it has no registry entry (priority < 0) or is TileType.None.
        // Hidden types are transparent to the renderer — they never create boundaries.
        private static bool IsHidden(TileColorRegistry reg, int t)
            => reg.IsNone(t) || reg.GetPriority(t) < 0;

        // True when all 4 cells are hidden → skip the tile entirely (no quad emitted).
        private static bool AllNone(TileColorRegistry reg, int a, int b, int c, int d)
            => IsHidden(reg, a) && IsHidden(reg, b) && IsHidden(reg, c) && IsHidden(reg, d);

        // Returns the highest-priority visible type among {a,b,c,d}, skipping `exclude`.
        // Returns TileType.None (0) if none qualify.
        private static int HighestPriority(TileColorRegistry reg, int a, int b, int c, int d, int exclude)
        {
            int best     = (int)TileType.None;
            int bestPrio = int.MinValue;
            foreach (int t in new[] { a, b, c, d })
            {
                if (IsHidden(reg, t) || t == exclude) continue;
                int p = reg.GetPriority(t);
                if (p > bestPrio) { bestPrio = p; best = t; }
            }
            return best;
        }

        private static void AppendQuad(
            List<Vector3> verts, List<Vector2> uvs,
            List<Color> primColors, List<Vector4> secColors,
            List<int> tris,
            float x0, float x1, float z0, float z1,
            Rect uv, Color primary, Color secondary)
        {
            int b = verts.Count;

            // Four corners on the XZ plane (Y = 0 in local chunk space).
            // Order: SW, SE, NW, NE — matching atlas orientation.
            verts.Add(new Vector3(x0, 0f, z0)); // SW
            verts.Add(new Vector3(x1, 0f, z0)); // SE
            verts.Add(new Vector3(x0, 0f, z1)); // NW
            verts.Add(new Vector3(x1, 0f, z1)); // NE

            uvs.Add(new Vector2(uv.xMin, uv.yMin)); // SW → atlas bottom-left
            uvs.Add(new Vector2(uv.xMax, uv.yMin)); // SE → atlas bottom-right
            uvs.Add(new Vector2(uv.xMin, uv.yMax)); // NW → atlas top-left
            uvs.Add(new Vector2(uv.xMax, uv.yMax)); // NE → atlas top-right

            var sec = new Vector4(secondary.r, secondary.g, secondary.b, secondary.a);
            primColors.Add(primary); primColors.Add(primary);
            primColors.Add(primary); primColors.Add(primary);
            secColors.Add(sec); secColors.Add(sec);
            secColors.Add(sec); secColors.Add(sec);

            // CW winding viewed from above (+Y).
            tris.Add(b);     tris.Add(b + 1); tris.Add(b + 2); // SW, SE, NW
            tris.Add(b + 1); tris.Add(b + 3); tris.Add(b + 2); // SE, NE, NW
        }

        private static void UploadMesh(
            MeshFilter filter,
            List<Vector3> verts, List<Vector2> uvs,
            List<Color> primColors, List<Vector4> secColors,
            List<int> tris)
        {
            var mesh = filter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh { name = "ChunkMesh" };
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                filter.sharedMesh = mesh;
            }

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(primColors);
            mesh.SetUVs(1, secColors);   // TEXCOORD1 → secondary tint in shader
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }
    }
}
