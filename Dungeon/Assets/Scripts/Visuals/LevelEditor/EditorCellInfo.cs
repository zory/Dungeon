using Dungeon.Logic;
using Dungeon.Logic.Services;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Level editor tool: displays hovered cell coordinates and tile info.
    // Only shows when at least one sibling editor tool GameObject is active.
    // Enable/disable via the GameObject's active state.
    public class EditorCellInfo : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private int _fontSize = 20;
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.75f);
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Vector2 _offset = new Vector2(20f, 20f);

        private GUIStyle _labelStyle;
        private GUIStyle _bgStyle;
        private string _text;

        private void Update()
        {
            if (!Application.isPlaying || GameBootstrapper.Instance == null) { return; }

            // Only show if at least one sibling editor tool is active.
            if (!AnyToolActive()) { _text = null; return; }

            GridService grid = GameBootstrapper.Instance.LogicWorld.Get<GridService>();
            Vector3Int? hovered = grid.HoveredCell;

            if (!hovered.HasValue) { _text = null; return; }

            Vector3Int cell = new Vector3Int(hovered.Value.x, grid.Elevation, hovered.Value.z);
            Cell gridCell = grid.Grid.GetCell(cell);
            int tileId = gridCell != null ? gridCell.TileId : -1;

            _text = $"Cell: ({cell.x}, {cell.y}, {cell.z})  Tile: {tileId}";
        }

        private bool AnyToolActive()
        {
            if (transform.parent == null) { return true; }

            foreach (Transform sibling in transform.parent)
            {
                if (sibling == transform) { continue; }
                if (sibling.gameObject.activeSelf)
                {
                    // Check if it has any editor tool component.
                    if (sibling.GetComponent<EditorTerrainBrush>() != null ||
                        sibling.GetComponent<EditorObjectBrush>() != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void OnGUI()
        {
            if (_text == null) { return; }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = _fontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(8, 8, 4, 4),
                };
                _labelStyle.normal.textColor = _textColor;
            }

            if (_bgStyle == null)
            {
                Texture2D bgTex = new Texture2D(1, 1);
                bgTex.SetPixel(0, 0, _backgroundColor);
                bgTex.Apply();

                _bgStyle = new GUIStyle
                {
                    normal = { background = bgTex },
                    padding = new RectOffset(8, 8, 4, 4),
                };
            }

            Vector2 mousePos = Event.current.mousePosition;
            Vector2 size = _labelStyle.CalcSize(new GUIContent(_text));
            Rect rect = new Rect(mousePos.x + _offset.x, mousePos.y + _offset.y, size.x, size.y);

            GUI.Box(rect, GUIContent.none, _bgStyle);
            GUI.Label(rect, _text, _labelStyle);
        }
    }
}
