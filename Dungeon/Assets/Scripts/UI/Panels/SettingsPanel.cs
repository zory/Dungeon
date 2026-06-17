using System;
using FactorialFun.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Dungeon.UI.Panels
{
    // In-game settings panel with Save and Close buttons.
    // Built programmatically — no prefab needed.
    public class SettingsPanel : PanelBase
    {
        public event Action OnSave;
        public event Action OnClose;

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

            // Semi-transparent overlay background (click-blocker).
            Image overlay = gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.6f);

            // Centered panel box.
            GameObject panel = CreateChild("Panel", rt);
            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(400f, 300f);

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.12f, 0.18f, 1f);

            // Title.
            GameObject titleGo = CreateChild("Title", panelRt);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 50f);

            Text titleText = titleGo.AddComponent<Text>();
            titleText.text = "Settings";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            // Close button (top-right corner).
            GameObject closeGo = CreateChild("CloseButton", panelRt);
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-8f, -8f);
            closeRt.sizeDelta = new Vector2(36f, 36f);

            Image closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.6f, 0.2f, 0.2f, 1f);

            Button closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => OnClose?.Invoke());

            GameObject closeLabelGo = CreateChild("Label", closeGo.transform);
            RectTransform closeLabelRt = closeLabelGo.GetComponent<RectTransform>();
            closeLabelRt.anchorMin = Vector2.zero;
            closeLabelRt.anchorMax = Vector2.one;
            closeLabelRt.offsetMin = Vector2.zero;
            closeLabelRt.offsetMax = Vector2.zero;

            Text closeLabel = closeLabelGo.AddComponent<Text>();
            closeLabel.text = "X";
            closeLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeLabel.fontSize = 20;
            closeLabel.fontStyle = FontStyle.Bold;
            closeLabel.alignment = TextAnchor.MiddleCenter;
            closeLabel.color = Color.white;

            // Save button (centered in panel).
            GameObject saveGo = CreateChild("SaveButton", panelRt);
            RectTransform saveRt = saveGo.GetComponent<RectTransform>();
            saveRt.anchorMin = new Vector2(0.5f, 0.5f);
            saveRt.anchorMax = new Vector2(0.5f, 0.5f);
            saveRt.pivot = new Vector2(0.5f, 0.5f);
            saveRt.sizeDelta = new Vector2(200f, 48f);

            Image saveImg = saveGo.AddComponent<Image>();
            saveImg.color = new Color(0.25f, 0.25f, 0.35f, 1f);

            Button saveBtn = saveGo.AddComponent<Button>();
            ColorBlock colors = saveBtn.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.50f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.25f, 1f);
            saveBtn.colors = colors;
            saveBtn.onClick.AddListener(() => OnSave?.Invoke());

            GameObject saveLabelGo = CreateChild("Label", saveGo.transform);
            RectTransform saveLabelRt = saveLabelGo.GetComponent<RectTransform>();
            saveLabelRt.anchorMin = Vector2.zero;
            saveLabelRt.anchorMax = Vector2.one;
            saveLabelRt.offsetMin = Vector2.zero;
            saveLabelRt.offsetMax = Vector2.zero;

            Text saveLabel = saveLabelGo.AddComponent<Text>();
            saveLabel.text = "Save Game";
            saveLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            saveLabel.fontSize = 22;
            saveLabel.alignment = TextAnchor.MiddleCenter;
            saveLabel.color = Color.white;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            return go;
        }
    }
}
