using System;
using System.Collections.Generic;

namespace Dungeon.Logic.Serialisation
{
    // Root object for a game save file.
    // Stores the generation seed so new chunks match the original world,
    // plus all cells that have been generated or modified.
    [Serializable]
    public class SaveData
    {
        public int             Seed  = 0;
        public List<CellData>  Cells = new List<CellData>();
    }
}
