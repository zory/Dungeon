using System;
using UnityEngine;

namespace Dungeon.Logic
{
    // Tracks hover, selection, and previous-selection state for the grid.
    // Visuals layer subscribes to the events; this class holds no rendering logic.
    public class GridManager : MonoBehaviour
    {
        // The one Grid instance for the scene — owned here so all MonoBehaviours
        // can reach it via a single inspector reference to GridManager.
        public CellGrid Grid { get; } = new CellGrid();

        public Vector3Int? HoveredCell          { get; private set; }
        public Vector3Int? SelectedCell         { get; private set; }
        public Vector3Int? PreviousSelectedCell { get; private set; }

        // Fired when the hovered cell changes (null = cursor left the grid)
        public event Action<Vector3Int?> OnHoverChanged;

        // Fired when selection changes; args are (previousCell, newCell), either may be null
        public event Action<Vector3Int?, Vector3Int?> OnSelectionChanged;

        public void SetHovered(Vector3Int? coord)
        {
            if (HoveredCell == coord) return;
            HoveredCell = coord;
            OnHoverChanged?.Invoke(coord);
        }

        public void Select(Vector3Int? coord)
        {
            if (SelectedCell == coord) return;
            PreviousSelectedCell = SelectedCell;
            SelectedCell = coord;
            OnSelectionChanged?.Invoke(PreviousSelectedCell, SelectedCell);
        }

        public void Deselect() => Select(null);
    }
}
