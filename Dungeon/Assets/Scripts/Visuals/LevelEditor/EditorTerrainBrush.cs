using Dungeon.Logic;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals
{
    // Level editor tool: paints terrain tiles on the grid.
    // Left-click = paint selected tile ID. Right-click = erase (set to empty).
    // Reads GridService.HoveredCell (populated by GridInputService each frame).
    // Enable/disable via the GameObject's active state.
    //
    // Terrain tiles represent the FLOOR at a given elevation, which is the
    // surface of the layer below.  Painting at elevation Y therefore defines
    // the material at Y-1.  When underground, painting also reveals the
    // painted cell and its 8 neighbours, and updates obstacle types one
    // level below to match the painted floor type.
    public class EditorTerrainBrush : MonoBehaviour
    {
        [Header("Brush Settings")]
        [Tooltip("Tile ID to paint. 0 = empty (erase). Use IDs from TerrainAtlas.")]
        [SerializeField] private int _tileId = 3;

        private GridService _grid;
        private WorldRenderService _worldRender;
        private UndergroundService _underground;
        private Vector3Int? _lastPaintedCell;

        private void Update()
        {
            if (!Application.isPlaying || GameBootstrapper.Instance == null) { return; }

            if (_grid == null)
            {
                _grid = GameBootstrapper.Instance.LogicWorld.Get<GridService>();
                _worldRender = GameBootstrapper.Instance.VisualWorld.Get<WorldRenderService>();
                _underground = GameBootstrapper.Instance.LogicWorld.Get<UndergroundService>();
            }

            Mouse mouse = Mouse.current;
            if (mouse == null) { return; }

            if (mouse.leftButton.wasReleasedThisFrame || mouse.rightButton.wasReleasedThisFrame)
            {
                _lastPaintedCell = null;
            }

            Vector3Int? hovered = _grid.HoveredCell;
            if (hovered == null) { return; }

            Vector3Int cell = new Vector3Int(hovered.Value.x, _grid.Elevation, hovered.Value.z);

            if (mouse.leftButton.isPressed)
            {
                if (cell == _lastPaintedCell) { return; }
                _lastPaintedCell = cell;
                PaintCell(cell, _tileId);
            }
            else if (mouse.rightButton.isPressed)
            {
                if (cell == _lastPaintedCell) { return; }
                _lastPaintedCell = cell;
                PaintCell(cell, 0);
            }
        }

        private void PaintCell(Vector3Int cell, int tileId)
        {
            _grid.Grid.SetCell(cell, new Cell(tileId));

            // When painting underground, the painted cell becomes dug (open space
            // showing the floor) and its 8 neighbours are revealed with obstacle
            // auto-generation for undug cells.
            if (UndergroundService.IsUnderground(cell.y) && _underground != null)
            {
                _underground.EditorRevealArea(cell);
            }

            // The floor at elevation Y represents the material at Y-1.
            // Update obstacle types one level below to match the painted floor.
            Vector3Int cellBelow = new Vector3Int(cell.x, cell.y - 1, cell.z);
            if (UndergroundService.IsUnderground(cellBelow.y) && _underground != null)
            {
                _underground.UpdateObstacleType(cellBelow, tileId);
            }

            RebuildChunkAt(cell);
        }

        private void RebuildChunkAt(Vector3Int cell)
        {
            Vector2Int chunk = GridService.CellToChunk(cell.x, cell.z);
            _worldRender.RebuildChunk(chunk);
            _worldRender.RebuildChunk(new Vector2Int(chunk.x - 1, chunk.y));
            _worldRender.RebuildChunk(new Vector2Int(chunk.x + 1, chunk.y));
            _worldRender.RebuildChunk(new Vector2Int(chunk.x, chunk.y - 1));
            _worldRender.RebuildChunk(new Vector2Int(chunk.x, chunk.y + 1));
        }
    }
}
