using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Listens to GridManager events and positions flat sprites in world space
    // to highlight hovered, selected, and previously-selected cells.
    // Sprites are rotated 90° on X so they lie on the XZ plane.
    public class GridHighlighter : MonoBehaviour
    {
        [SerializeField] private GridManager  _gridManager;
        [SerializeField] private GridRenderer _gridRenderer;

        [Header("Sprites")]
        [SerializeField] private Sprite _hoverSprite;
        [SerializeField] private Sprite _selectedSprite;
        [SerializeField] private Sprite _prevSelectedSprite;

        [Header("Colors")]
        [SerializeField] private Color _hoverColor       = new Color(1.00f, 1.00f, 0.00f, 0.50f);
        [SerializeField] private Color _selectedColor    = new Color(0.00f, 1.00f, 0.00f, 0.70f);
        [SerializeField] private Color _prevSelectedColor = new Color(0.20f, 0.60f, 1.00f, 0.40f);

        [Header("Sorting")]
        [SerializeField] private int _sortingOrder = 10;

        private SpriteRenderer _hoverSr;
        private SpriteRenderer _selectedSr;
        private SpriteRenderer _prevSelectedSr;

        private void Awake()
        {
            _hoverSr        = CreateHighlight("Hover",        _hoverSprite,        _hoverColor);
            _selectedSr     = CreateHighlight("Selected",     _selectedSprite,     _selectedColor);
            _prevSelectedSr = CreateHighlight("PrevSelected", _prevSelectedSprite, _prevSelectedColor);
        }

        private void OnEnable()
        {
            _gridManager.OnHoverChanged     += HandleHoverChanged;
            _gridManager.OnSelectionChanged += HandleSelectionChanged;

            // Sync immediately in case state existed before this component enabled
            HandleHoverChanged(_gridManager.HoveredCell);
            HandleSelectionChanged(_gridManager.PreviousSelectedCell, _gridManager.SelectedCell);
        }

        private void OnDisable()
        {
            _gridManager.OnHoverChanged     -= HandleHoverChanged;
            _gridManager.OnSelectionChanged -= HandleSelectionChanged;
        }

        private void HandleHoverChanged(Vector3Int? cell)
            => PlaceHighlight(_hoverSr, cell);

        private void HandleSelectionChanged(Vector3Int? prev, Vector3Int? current)
        {
            PlaceHighlight(_selectedSr,     current);
            PlaceHighlight(_prevSelectedSr, prev);
        }

        private void PlaceHighlight(SpriteRenderer sr, Vector3Int? cell)
        {
            if (cell.HasValue)
            {
                sr.gameObject.SetActive(true);
                sr.transform.position = CellCenter(cell.Value);
            }
            else
            {
                sr.gameObject.SetActive(false);
            }
        }

        // Returns the world-space center of a cell on the grid plane
        private Vector3 CellCenter(Vector3Int cell)
        {
            float s = _gridRenderer.CellSize;
            float x = (cell.x + 0.5f) * s + _gridRenderer.XZOffset.x;
            float z = (cell.z + 0.5f) * s + _gridRenderer.XZOffset.y;
            return new Vector3(x, _gridRenderer.WorldY, z);
        }

        private SpriteRenderer CreateHighlight(string label, Sprite sprite, Color color)
        {
            var go = new GameObject($"GridHighlight_{label}");
            go.transform.SetParent(transform, worldPositionStays: false);
            // Rotate so the sprite lies flat on the XZ plane (visible from above)
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.SetActive(false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.color        = color;
            sr.sortingOrder = _sortingOrder;

            // Scale the sprite to fill exactly one cell.
            // Expects square sprites at any PPU; non-square sprites will stretch to fit the cell.
            float s = _gridRenderer.CellSize;
            if (sprite != null)
            {
                float ppu = sprite.pixelsPerUnit;
                float sw  = sprite.rect.width  / ppu;
                float sh  = sprite.rect.height / ppu;
                // After Euler(90,0,0): localScale.x → world X, localScale.y → world Z
                go.transform.localScale = new Vector3(s / sw, s / sh, 1f);
            }
            else
            {
                go.transform.localScale = new Vector3(s, s, 1f);
            }

            return sr;
        }
    }
}
