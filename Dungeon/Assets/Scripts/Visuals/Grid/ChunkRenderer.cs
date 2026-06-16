using System.Collections.Generic;
using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Builds a layered mesh for one chunk using dual-grid autotiling.
    //
    // DUAL-GRID OVERVIEW
    // ------------------
    // The data grid stores one TileType per cell (x, y, z).
    // Each visual tile covers the same world area as a data cell but looks at FOUR
    // surrounding data cells to compute a 4-bit bitmask that selects a shape from
    // the DualGridAtlas.
    //
    //   Visual tile at local (i, j) samples:
    //     SW = cell (baseX + i,     y, baseZ + j    )
    //     SE = cell (baseX + i + 1, y, baseZ + j    )
    //     NW = cell (baseX + i,     y, baseZ + j + 1)
    //     NE = cell (baseX + i + 1, y, baseZ + j + 1)
    //
    // Bitmask bits:  NW=1, NE=2, SW=4, SE=8
    //
    // PER-LAYER RENDERING
    // -------------------
    // Each visible terrain type gets its own sub-mesh layer.  Layers are ordered by
    // priority (lowest first) and rendered with alpha blending, so higher-priority
    // terrain draws on top and any number of types can meet at a single tile.
    //
    // The shape atlas (_MainTex) defines terrain presence via alpha (1 inside,
    // 0 outside).  An optional outline atlas (_OutlineTex) adds border lines.
    // The complement trick (8 tiles cover 16 bitmasks) is handled by an invert
    // flag passed per vertex — the shader flips the alpha when set.
    //
    // HIDDEN TYPES
    // ------------
    // Types not registered in TileColorRegistry (e.g. Tree, Darkness) are "hidden."
    // Hidden cells extend whichever terrain types are present at adjacent corners,
    // but only if that terrain type actually appears in at least one non-hidden
    // corner of the tile.
    public class DualGridChunkRenderer : MonoBehaviour
    {
        public const int ChunkSize = WorldConstants.ChunkSize;

        private const int BIT_NW = 1 << 0; // 1
        private const int BIT_NE = 1 << 1; // 2
        private const int BIT_SW = 1 << 2; // 4
        private const int BIT_SE = 1 << 3; // 8

        private MeshFilter   _filter;
        private MeshRenderer _renderer;

        private static Texture2D s_clearTexture;

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
            CellGrid          grid,
            int               elevation,
            float             cellSize,
            DualGridAtlas     atlas,
            TileColorRegistry colorRegistry,
            Material          material)
        {
            if (atlas.Atlas != null)
                material.mainTexture = atlas.Atlas;
            material.SetTexture("_OutlineTex",
                atlas.OutlineAtlas != null ? atlas.OutlineAtlas : GetClearTexture());

            int baseX = chunkCoord.x * ChunkSize;
            int baseZ = chunkCoord.y * ChunkSize;

            // ── Pass 1: collect all visible terrain types (including border cells). ──

            var terrainTypes = new HashSet<int>();
            for (int j = 0; j <= ChunkSize; j++)
            for (int i = 0; i <= ChunkSize; i++)
            {
                int t = CellType(grid, baseX + i, elevation, baseZ + j);
                if (!IsHidden(colorRegistry, t))
                    terrainTypes.Add(t);
            }

            if (terrainTypes.Count == 0) { ClearMesh(); return; }

            // Lowest priority first → bottom layer → rendered first.
            var sortedTypes = new List<int>(terrainTypes);
            sortedTypes.Sort((a, b) =>
                colorRegistry.GetPriority(a).CompareTo(colorRegistry.GetPriority(b)));

            // ── Pass 2: build per-layer mesh data. ──────────────────────────────────

            var verts  = new List<Vector3>();
            var uvs    = new List<Vector2>();
            var colors = new List<Color>();
            var flags  = new List<Vector4>();
            var layerTris = new List<List<int>>();

            foreach (int terrainType in sortedTypes)
            {
                var   tris   = new List<int>();
                Color tColor = colorRegistry.GetColor(terrainType);

                for (int j = 0; j < ChunkSize; j++)
                for (int i = 0; i < ChunkSize; i++)
                {
                    int gx = baseX + i;
                    int gz = baseZ + j;

                    int swType = CellType(grid, gx,     elevation, gz    );
                    int seType = CellType(grid, gx + 1, elevation, gz    );
                    int nwType = CellType(grid, gx,     elevation, gz + 1);
                    int neType = CellType(grid, gx + 1, elevation, gz + 1);

                    // Raw mask: only corners that are exactly this terrain type.
                    int rawMask = 0;
                    if (nwType == terrainType) rawMask |= BIT_NW;
                    if (neType == terrainType) rawMask |= BIT_NE;
                    if (swType == terrainType) rawMask |= BIT_SW;
                    if (seType == terrainType) rawMask |= BIT_SE;

                    if (rawMask == 0) continue; // type not present at this tile

                    // Hidden corners extend coverage for present types.
                    int hiddenMask = 0;
                    if (IsHidden(colorRegistry, nwType)) hiddenMask |= BIT_NW;
                    if (IsHidden(colorRegistry, neType)) hiddenMask |= BIT_NE;
                    if (IsHidden(colorRegistry, swType)) hiddenMask |= BIT_SW;
                    if (IsHidden(colorRegistry, seType)) hiddenMask |= BIT_SE;

                    int  mask   = rawMask | hiddenMask;
                    Rect uvRect = atlas.GetUVRect(mask, out bool swap);

                    float x0 = i * cellSize, x1 = x0 + cellSize;
                    float z0 = j * cellSize, z1 = z0 + cellSize;

                    AppendQuad(verts, uvs, colors, flags, tris,
                               x0, x1, z0, z1, uvRect, tColor, swap);
                }

                if (tris.Count > 0)
                    layerTris.Add(tris);
            }

            if (layerTris.Count == 0) { ClearMesh(); return; }

            UploadMesh(_filter, verts, uvs, colors, flags, layerTris);

            var materials = new Material[layerTris.Count];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;
            _renderer.sharedMaterials = materials;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void ClearMesh()
        {
            if (_filter.sharedMesh != null)
                _filter.sharedMesh.Clear();
            _renderer.sharedMaterials = System.Array.Empty<Material>();
        }

        private static Texture2D GetClearTexture()
        {
            if (s_clearTexture == null)
            {
                s_clearTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                s_clearTexture.SetPixel(0, 0, Color.clear);
                s_clearTexture.Apply();
                s_clearTexture.hideFlags = HideFlags.HideAndDontSave;
            }
            return s_clearTexture;
        }

        private static int CellType(CellGrid grid, int x, int y, int z)
            => grid.GetCell(new Vector3Int(x, y, z))?.TileId ?? (int)TileType.None;

        private static bool IsHidden(TileColorRegistry reg, int t)
            => reg.IsNone(t) || reg.GetPriority(t) < 0;

        private static void AppendQuad(
            List<Vector3> verts, List<Vector2> uvs,
            List<Color> colors, List<Vector4> flags,
            List<int> tris,
            float x0, float x1, float z0, float z1,
            Rect uv, Color color, bool swap)
        {
            int b = verts.Count;

            // Four corners on the XZ plane (Y = 0 in local chunk space).
            verts.Add(new Vector3(x0, 0f, z0)); // SW
            verts.Add(new Vector3(x1, 0f, z0)); // SE
            verts.Add(new Vector3(x0, 0f, z1)); // NW
            verts.Add(new Vector3(x1, 0f, z1)); // NE

            uvs.Add(new Vector2(uv.xMin, uv.yMin));
            uvs.Add(new Vector2(uv.xMax, uv.yMin));
            uvs.Add(new Vector2(uv.xMin, uv.yMax));
            uvs.Add(new Vector2(uv.xMax, uv.yMax));

            var flag = new Vector4(swap ? 1f : 0f, 0f, 0f, 0f);
            colors.Add(color); colors.Add(color);
            colors.Add(color); colors.Add(color);
            flags.Add(flag);  flags.Add(flag);
            flags.Add(flag);  flags.Add(flag);

            // CW winding viewed from above (+Y).
            tris.Add(b);     tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b + 1); tris.Add(b + 3); tris.Add(b + 2);
        }

        private static void UploadMesh(
            MeshFilter filter,
            List<Vector3> verts, List<Vector2> uvs,
            List<Color> colors, List<Vector4> flags,
            List<List<int>> layerTris)
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
            mesh.SetColors(colors);
            mesh.SetUVs(1, flags);    // TEXCOORD1 → invert flag in shader
            mesh.subMeshCount = layerTris.Count;
            for (int i = 0; i < layerTris.Count; i++)
                mesh.SetTriangles(layerTris[i], i);
            mesh.RecalculateBounds();
        }
    }
}
