namespace Dungeon.Logic
{
    // Named tile types used throughout the logic layer.
    // None and Darkness are reserved:
    //   None (0)     — no tile, the cell is empty and will not be rendered
    //   Darkness (1) — default opaque tile shown where nothing has been revealed yet
    // All other values are terrain / object types.
    public enum TileType
    {
        None     = 0,
        Darkness = 1,
        Water    = 2,
        Grass    = 3,
        Dirt     = 4,
        Rock     = 5,
        Tree     = 6,
    }
}
