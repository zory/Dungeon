using System.Collections.Generic;
using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Central definition of all terrain types and their atlas tile mappings.
    //
    // ONE TEXTURE — the terrain atlas
    // ────────────────────────────────
    //   A single sprite-sheet holding every terrain type's tiles.
    //   Full RGBA per tile:
    //     Alpha = terrain presence (1 inside shape, 0 outside).
    //     RGB   = painted appearance, including any outlines/borders
    //             baked directly into the artwork.
    //
    // 16 TILES PER TERRAIN TYPE (asymmetric — no rotation)
    // ────────────────────────────────────────────────────
    //   Each terrain type has 16 consecutive tiles in the atlas, one for
    //   every possible 4-bit corner bitmask (0–15).  Tiles are hand-painted
    //   for each configuration — the shader does NOT rotate UVs.
    //
    //   Bitmask bits: NW=1, NE=2, SW=4, SE=8.
    //
    //   bm  corners         description
    //    0  (empty)         no corners filled (never rendered)
    //    1  NW              single corner — northwest
    //    2  NE              single corner — northeast
    //    3  NW+NE           north edge
    //    4  SW              single corner — southwest
    //    5  NW+SW           west edge
    //    6  NE+SW           diagonal (NE-SW)
    //    7  NW+NE+SW        T-shape, missing SE
    //    8  SE              single corner — southeast
    //    9  NW+SE           diagonal (NW-SE)
    //   10  NE+SE           east edge
    //   11  NW+NE+SE        T-shape, missing SW
    //   12  SW+SE           south edge
    //   13  NW+SW+SE        T-shape, missing NE
    //   14  NE+SW+SE        T-shape, missing NW
    //   15  all             full tile — all corners filled
    //
    //   Paint each tile exactly as it should appear on screen.
    //   Transparent (alpha = 0) where the terrain is NOT present.
    //   Paint outlines/borders directly into the tile RGB.
    [CreateAssetMenu(menuName = "Dungeon/Terrain Atlas", fileName = "TerrainAtlas")]
    public class TerrainAtlas : ScriptableObject
    {
        // ── Constants ────────────────────────────────────────────────────────

        // Number of tiles per terrain type (one per bitmask 0–15).
        public const int TILES_PER_TERRAIN = 16;

        // Virtual tile ID used by the renderer for unrevealed underground cells.
        // Never stored in grid data — purely a rendering substitute.
        public const int NOT_REVEALED_ID = int.MinValue;

        // ── Serialised fields ────────────────────────────────────────────────

        [Header("Terrain Atlas")]
        [SerializeField] private Texture2D _texture;
        [SerializeField] [Min(1)] private int _columns = 16;
        [SerializeField] [Min(1)] private int _rows    = 4;

        [Header("Terrain Types")]
        [SerializeField] private List<TerrainEntry> _terrains = new();

        [Header("Special IDs")]
        [Tooltip("Tile ID for cells that were never generated. Must match the " +
                 "fallback used when no cell exists in the grid.")]
        [SerializeField] private int _notInitializedId = -1;

        [Tooltip("Tile ID for empty cells. Used when terrain " +
                 "is removed, e.g. after digging out an obstacle.")]
        [SerializeField] private int _emptyId = 0;

        [Tooltip("First tile index in the atlas for empty terrain. " +
                 "16 bitmask variants (0–15) laid out consecutively.")]
        [SerializeField] private int _emptyFirstTileIndex;

        [Tooltip("Colour tint for empty terrain tiles.")]
        [SerializeField] private Color _emptyColor = Color.black;

        [Tooltip("Render priority for empty terrain (higher = drawn on top).")]
        [SerializeField] private int _emptyPriority = 0;

        [Header("Wall Atlas")]
        [Tooltip("Texture atlas for wall tiles (obstacle autotiling).")]
        [SerializeField] private Texture2D _wallTexture;
        [SerializeField] [Min(1)] private int _wallColumns = 16;
        [SerializeField] [Min(1)] private int _wallRows    = 4;

        [Header("Not Revealed (visual-only fog-of-war override)")]
        [Tooltip("First tile index in the atlas for unrevealed rendering. " +
                 "16 bitmask variants (0–15) laid out consecutively.")]
        [SerializeField] private int _notRevealedFirstTileIndex;

        [Tooltip("Colour tint for unrevealed tiles.")]
        [SerializeField] private Color _notRevealedColor = Color.black;

        [Tooltip("Render priority for unrevealed tiles (higher = drawn on top).")]
        [SerializeField] private int _notRevealedPriority = 9999;

        // ── Public accessors ─────────────────────────────────────────────────

        public Texture2D Texture          => _texture;
        public int       Columns          => _columns;
        public int       Rows             => _rows;
        public int       NotInitializedId => _notInitializedId;
        public int       EmptyId          => _emptyId;

        public Texture2D WallTexture      => _wallTexture;
        public int       WallColumns      => _wallColumns;
        public int       WallRows         => _wallRows;

        // ── Terrain entry ────────────────────────────────────────────────────

        [System.Serializable]
        public struct TerrainEntry
        {
            public int    Id;
            public string Name;

            [Tooltip("Higher = drawn on top at transitions.  Negative = hidden " +
                     "(not rendered; its corners extend neighbouring visible types).")]
            public int    Priority;

            [Tooltip("Fallback flat colour used by non-rendering systems " +
                     "(e.g. obstacle sprites).")]
            public Color  Color;

            [Header("Atlas mapping")]

            [Tooltip("Index of the first tile in the atlas for this terrain type. " +
                     "The 16 bitmask variants (0–15) must be laid out consecutively " +
                     "starting at this index.  Bitmask bits: NW=1, NE=2, SW=4, SE=8.")]
            public int FirstTileIndex;

            [Tooltip("Index of the first tile in the wall atlas for this terrain type. " +
                     "The 16 cardinal-neighbor bitmask variants (0–15) are laid out " +
                     "consecutively.  Bitmask bits: N=1, E=2, S=4, W=8.  " +
                     "Set to -1 if this type has no wall tiles.")]
            public int WallFirstTileIndex;

            [Header("Generation — Surface")]

            [Tooltip("Surface noise height band [0..1].  Set both to -1 to exclude " +
                     "this terrain from surface generation.")]
            [Range(-1f, 1.01f)] public float SurfaceHeightMin;
            [Range(-1f, 1.01f)] public float SurfaceHeightMax;

            [Header("Generation — Underground")]

            [Tooltip("Underground depth range (1 = first level below surface).  " +
                     "Both 0 = this terrain does not appear underground.")]
            [Min(0)] public int UndergroundDepthMin;
            [Min(0)] public int UndergroundDepthMax;

            [Tooltip("If true, fills the entire underground column wherever the " +
                     "surface tile is this type (e.g. water extends down).")]
            public bool ExtendsUnderground;

            [Header("Generation — Secondary Noise Overlay")]

            [Tooltip("Secondary noise scale (0 = disabled).  Used for overlay " +
                     "patterns like trees on grass.")]
            [Min(0f)] public float SecondaryNoiseScale;

            [Tooltip("Noise threshold to trigger the replacement tile.")]
            [Range(0f, 1f)] public float SecondaryNoiseThreshold;

            [Tooltip("Tile ID placed when secondary noise exceeds threshold.")]
            public int SecondaryTileId;
        }

        // ── Lookup ───────────────────────────────────────────────────────────

        private Dictionary<int, TerrainEntry> _lookup;

        private void OnEnable() => RebuildLookup();

        private void RebuildLookup()
        {
            _lookup = new Dictionary<int, TerrainEntry>(_terrains.Count);
            foreach (TerrainEntry entry in _terrains)
            {
                _lookup[entry.Id] = entry;
            }
        }

        private void EnsureLookup()
        {
            if (_lookup == null) { RebuildLookup(); }
        }

        public string GetName(int tileId)
        {
            EnsureLookup();
            return _lookup.TryGetValue(tileId, out TerrainEntry entry) ? entry.Name : null;
        }

        public int GetPriority(int tileId)
        {
            if (tileId == NOT_REVEALED_ID) { return _notRevealedPriority; }
            if (tileId == _emptyId) { return _emptyPriority; }
            EnsureLookup();
            return _lookup.TryGetValue(tileId, out TerrainEntry entry) ? entry.Priority : -1;
        }

        public Color GetColor(int tileId)
        {
            if (tileId == NOT_REVEALED_ID) { return _notRevealedColor; }
            if (tileId == _emptyId) { return _emptyColor; }
            EnsureLookup();
            return _lookup.TryGetValue(tileId, out TerrainEntry entry) ? entry.Color : Color.clear;
        }

        public bool TryGetEntry(int tileId, out TerrainEntry entry)
        {
            EnsureLookup();
            return _lookup.TryGetValue(tileId, out entry);
        }

        // ── Bitmask → tile index (direct, no rotation) ───────────────────────

        /// <summary>
        /// Given a terrain type Id and its 4-bit corner bitmask, returns the
        /// atlas tile index.  Rotation is always 0 — each bitmask has its own
        /// pre-painted tile, no UV rotation needed.
        /// Returns false if the terrain Id is not registered.
        /// </summary>
        public bool GetTileInfo(int tileId, int bitmask,
                                out int terrainTileIndex, out int rotation)
        {
            rotation = 0;
            if (tileId == NOT_REVEALED_ID)
            {
                terrainTileIndex = _notRevealedFirstTileIndex + (bitmask & 0xF);
                return true;
            }
            if (tileId == _emptyId)
            {
                terrainTileIndex = _emptyFirstTileIndex + (bitmask & 0xF);
                return true;
            }
            if (TryGetEntry(tileId, out TerrainEntry entry))
            {
                terrainTileIndex = entry.FirstTileIndex + (bitmask & 0xF);
                return true;
            }
            terrainTileIndex = 0;
            return false;
        }

        // ── Wall bitmask → tile index (cardinal neighbors, no rotation) ─────

        /// <summary>
        /// Given a terrain/obstacle type Id and its 4-bit cardinal bitmask,
        /// returns the wall atlas tile index.
        /// Bitmask bits: N=1, E=2, S=4, W=8.
        /// Returns false if the type has no wall tiles or is not registered.
        /// </summary>
        public bool GetWallTileInfo(int tileId, int bitmask, out int wallTileIndex)
        {
            EnsureLookup();
            if (_lookup.TryGetValue(tileId, out TerrainEntry entry) && entry.WallFirstTileIndex >= 0)
            {
                wallTileIndex = entry.WallFirstTileIndex + (bitmask & 0xF);
                return true;
            }
            wallTileIndex = 0;
            return false;
        }

        // ── Generation rules bridge (Visuals → Logic) ───────────────────────

        /// <summary>
        /// Converts all terrain entries into Logic-layer generation rules.
        /// Called at bootstrap to pass data-driven terrain config to
        /// WorldGenerationService without a Visuals dependency.
        /// </summary>
        public TerrainGenerationRule[] GetGenerationRules()
        {
            TerrainGenerationRule[] rules = new TerrainGenerationRule[_terrains.Count];
            for (int i = 0; i < _terrains.Count; i++)
            {
                TerrainEntry entry = _terrains[i];
                rules[i] = new TerrainGenerationRule
                {
                    TileId = entry.Id,
                    SurfaceHeightMin = entry.SurfaceHeightMin,
                    SurfaceHeightMax = entry.SurfaceHeightMax,
                    UndergroundDepthMin = entry.UndergroundDepthMin,
                    UndergroundDepthMax = entry.UndergroundDepthMax,
                    ExtendsUnderground = entry.ExtendsUnderground,
                    SecondaryNoiseScale = entry.SecondaryNoiseScale,
                    SecondaryNoiseThreshold = entry.SecondaryNoiseThreshold,
                    SecondaryTileId = entry.SecondaryTileId,
                };
            }
            return rules;
        }
    }
}
