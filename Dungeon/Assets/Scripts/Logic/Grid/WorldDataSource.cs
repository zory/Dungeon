using UnityEngine;

namespace Dungeon.Logic
{
    // Implement this on any MonoBehaviour to feed tile data into WorldGenerator.
    // Examples: procedural noise, loaded save file, network stream, etc.
    public abstract class WorldDataSource : MonoBehaviour
    {
        // Called once per cell during generation. Must be deterministic for a given coord.
        public abstract int GetTileId(int x, int y, int z);
    }
}
