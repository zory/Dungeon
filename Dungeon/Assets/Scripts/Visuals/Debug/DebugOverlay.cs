using Dungeon.Logic.Services;
using UnityEngine;

namespace Dungeon.Visuals.DebugTools
{
    // Shows the hovered cell's Vector3Int coordinates on screen.
    // Enable/disable via the GameObject's active state in the hierarchy.
    public class DebugOverlay : MonoBehaviour
    {
        [SerializeField] private int _fontSize = 22;
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.6f);

        private GUIStyle _labelStyle;
        private GUIStyle _bgStyle;

        private void OnGUI()
        {
            if (GameBootstrapper.Instance == null) { return; }

            GridService grid = GameBootstrapper.Instance.LogicWorld.Get<GridService>();
            if (grid == null) { return; }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = _fontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                };
                _labelStyle.normal.textColor = _textColor;

                _bgStyle = new GUIStyle(GUI.skin.box);
                Texture2D bgTex = new Texture2D(1, 1);
                bgTex.SetPixel(0, 0, _backgroundColor);
                bgTex.Apply();
                _bgStyle.normal.background = bgTex;
            }

            Vector3Int? hovered = grid.HoveredCell;
            if (!hovered.HasValue) { return; }

            Vector3Int cell = hovered.Value;
            string text = $"  Cell: ({cell.x}, {cell.y}, {cell.z})  ";

            Vector2 size = _labelStyle.CalcSize(new GUIContent(text));
            float padding = 4f;
            Rect bgRect = new Rect(10f, 10f, size.x + padding * 2f, size.y + padding * 2f);
            Rect textRect = new Rect(10f + padding, 10f + padding, size.x, size.y);

            GUI.Box(bgRect, GUIContent.none, _bgStyle);
            GUI.Label(textRect, text, _labelStyle);
        }
    }
}
