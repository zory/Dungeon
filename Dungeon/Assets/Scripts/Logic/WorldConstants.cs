namespace Dungeon.Logic
{
    public static class WorldConstants
    {
        // Number of cells per chunk side. Shared between Logic (ChunkLoader)
        // and Visuals (ChunkRenderer) so both always agree on chunk boundaries.
        public const int ChunkSize = 16;
    }
}
