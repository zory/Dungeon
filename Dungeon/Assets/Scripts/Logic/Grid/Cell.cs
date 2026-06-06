namespace Dungeon.Logic
{
    public class Cell
    {
        public int TileId { get; set; }

        public Cell(int tileId = 0) => TileId = tileId;
    }
}
