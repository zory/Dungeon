using Dungeon.Visuals.Services;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    public class GridHighlightAuthoring : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] private Sprite _hoverSprite;
        [SerializeField] private Sprite _selectedSprite;
        [SerializeField] private Sprite _prevSelectedSprite;

        [Header("Colors")]
        [SerializeField] private Color _hoverColor = new Color(1.00f, 1.00f, 0.00f, 0.50f);
        [SerializeField] private Color _selectedColor = new Color(0.00f, 1.00f, 0.00f, 0.70f);
        [SerializeField] private Color _prevSelectedColor = new Color(0.20f, 0.60f, 1.00f, 0.40f);

        [Header("Sorting")]
        [SerializeField] private int _sortingOrder = 10;

        public GridHighlightConfig GetConfig() => new GridHighlightConfig
        {
            HoverSprite = _hoverSprite,
            SelectedSprite = _selectedSprite,
            PrevSelectedSprite = _prevSelectedSprite,
            HoverColor = _hoverColor,
            SelectedColor = _selectedColor,
            PrevSelectedColor = _prevSelectedColor,
            SortingOrder = _sortingOrder,
        };
    }
}
