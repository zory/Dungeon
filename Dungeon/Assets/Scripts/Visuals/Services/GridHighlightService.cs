using System;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using UnityEngine;

namespace Dungeon.Visuals.Services
{
    [Serializable]
    public struct GridHighlightConfig
    {
        public Sprite HoverSprite;
        public Sprite SelectedSprite;
        public Sprite PrevSelectedSprite;

        public Color HoverColor;
        public Color SelectedColor;
        public Color PrevSelectedColor;

        public int SortingOrder;

        public static GridHighlightConfig Default => new GridHighlightConfig
        {
            HoverColor = new Color(1.00f, 1.00f, 0.00f, 0.50f),
            SelectedColor = new Color(0.00f, 1.00f, 0.00f, 0.70f),
            PrevSelectedColor = new Color(0.20f, 0.60f, 1.00f, 0.40f),
            SortingOrder = 10,
        };
    }

    public class GridHighlightService : IVisualService
    {
        private readonly GridHighlightConfig _config;
        private VisualWorld _world;
        private GridService _grid;

        private GameObject _root;
        private SpriteRenderer _hoverSr;
        private SpriteRenderer _selectedSr;
        private SpriteRenderer _prevSelectedSr;

        public GridHighlightService(GridHighlightConfig config)
        {
            _config = config;
        }

        public void Initialize(VisualWorld world)
        {
            _world = world;
            _grid = world.GetLogic<GridService>();

            _root = new GameObject("GridHighlights");

            _hoverSr        = CreateHighlight("Hover",        _config.HoverSprite,        _config.HoverColor);
            _selectedSr     = CreateHighlight("Selected",     _config.SelectedSprite,     _config.SelectedColor);
            _prevSelectedSr = CreateHighlight("PrevSelected", _config.PrevSelectedSprite, _config.PrevSelectedColor);

            _grid.OnHoverChanged     += HandleHoverChanged;
            _grid.OnSelectionChanged += HandleSelectionChanged;
        }

        public void Tick(float deltaTime) { }

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
                sr.transform.position = _grid.CellCenter(cell.Value);
            }
            else
            {
                sr.gameObject.SetActive(false);
            }
        }

        private SpriteRenderer CreateHighlight(string label, Sprite sprite, Color color)
        {
            var go = new GameObject($"GridHighlight_{label}");
            go.transform.SetParent(_root.transform, worldPositionStays: false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.SetActive(false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.color        = color;
            sr.sortingOrder = _config.SortingOrder;

            float s = _grid.CellSize;
            if (sprite != null)
            {
                float ppu = sprite.pixelsPerUnit;
                float sw  = sprite.rect.width  / ppu;
                float sh  = sprite.rect.height / ppu;
                go.transform.localScale = new Vector3(s / sw, s / sh, 1f);
            }
            else
            {
                go.transform.localScale = new Vector3(s, s, 1f);
            }

            return sr;
        }

        public void Dispose()
        {
            if (_grid != null)
            {
                _grid.OnHoverChanged     -= HandleHoverChanged;
                _grid.OnSelectionChanged -= HandleSelectionChanged;
            }

            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }
    }
}
