using System;

namespace Dungeon.Logic
{
    // Carries per-terrain generation parameters from the terrain database to
    // WorldGenerationService.  Defined in Logic so the generator has no
    // dependency on Visuals.
    [Serializable]
    public struct TerrainGenerationRule
    {
        // Which tile ID this rule produces.
        public int TileId;

        // Surface generation: noise height band [0..1] for placement at y==0.
        // Both -1 means this terrain does not appear on the surface.
        public float SurfaceHeightMin;
        public float SurfaceHeightMax;

        // Underground generation: depth range (1 = first level below surface).
        // Both 0 means this terrain does not appear underground.
        public int UndergroundDepthMin;
        public int UndergroundDepthMax;

        // When true, fills the entire underground column wherever the surface
        // tile is this type (e.g. water extends down).
        public bool ExtendsUnderground;

        // Secondary noise overlay within this terrain's surface band.
        // When secondary noise exceeds the threshold, SecondaryTileId is placed
        // instead (e.g. trees on grass).  Set SecondaryNoiseScale to 0 to disable.
        public float SecondaryNoiseScale;
        public float SecondaryNoiseThreshold;
        public int SecondaryTileId;
    }
}
