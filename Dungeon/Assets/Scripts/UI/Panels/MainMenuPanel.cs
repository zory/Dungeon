using System;
using FactorialFun.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Dungeon.UI.Panels
{
    // Main menu panel with New Game, Load Game, and Exit buttons.
    // Buttons are built programmatically — no prefab needed.
    public class MainMenuPanel : PanelBase
    {
        public event Action OnNewGame;
        public event Action OnLoadGame;
        public event Action OnExit;

        private void Awake()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            RectTransform rt = GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Dark semi-transparent background.
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // Vertical layout container centered on screen.
            GameObject container = CreateChild("ButtonContainer", rt);
            RectTransform containerRt = container.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.sizeDelta = new Vector2(320f, 280f);

            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Title.
            CreateLabel(container.transform, "Title", "DUNGEON", 36, FontStyle.Bold);

            // Spacer.
            GameObject spacer = CreateChild("Spacer", container.transform);
            LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.preferredHeight = 20f;

            // Buttons.
            CreateButton(container.transform, "NewGameButton", "New Game", () => OnNewGame?.Invoke());
            CreateButton(container.transform, "LoadGameButton", "Load Game", () => OnLoadGame?.Invoke());
            CreateButton(container.transform, "ExitButton", "Exit", () => OnExit?.Invoke());
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            return go;
        }

        private static void CreateLabel(Transform parent, string name, string text, int fontSize, FontStyle style)
        {
            GameObject go = CreateChild(name, parent);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = fontSize + 16f;

            Text label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        private static void CreateButton(Transform parent, string name, string label, Action onClick)
        {
            GameObject go = CreateChild(name, parent);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 48f;

            Image img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.35f, 1f);

            Button btn = go.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.50f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.25f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());

            // Button label.
            GameObject textGo = CreateChild("Label", go.transform);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            Text txt = textGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 22;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }
    }
}
