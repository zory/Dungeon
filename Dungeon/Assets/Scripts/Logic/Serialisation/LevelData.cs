using System;
using System.Collections.Generic;

namespace Dungeon.Logic.Serialisation
{
    // Root object for a serialised level file.
    // A single file can contain cells and objects at multiple elevation layers (Y stores elevation).
    [Serializable]
    public class LevelData
    {
        public LevelMetadata     Metadata = new LevelMetadata();
        public List<CellData>    Cells    = new List<CellData>();
        public List<ObjectData>  Objects  = new List<ObjectData>();
    }

    [Serializable]
    public class LevelMetadata
    {
        public string Name    = "unnamed";
        public int    Version = 1;
    }

    // One authored cell.  Y == elevation layer (0 = surface, -1 = first underground floor, …).
    [Serializable]
    public class CellData
    {
        public int X;
        public int Y;
        public int Z;
        public int TileTypeId;
    }

    // One authored world object.  TypeId is looked up in WorldObjectDatabase at load time.
    [Serializable]
    public class ObjectData
    {
        public string TypeId;
        public int    X;
        public int    Y;
        public int    Z;
    }
}
