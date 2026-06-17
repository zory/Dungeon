using System.IO;
using Dungeon.Logic;
using Dungeon.Logic.Serialisation;
using Dungeon.Logic.Services;
using Dungeon.UI.Panels;
using Dungeon.Visuals;
using FactorialFun.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Dungeon.UI
{
    // Placed in the Gameplay scene. Creates a settings toggle button and the settings panel.
    public class GameplayUISetup : MonoBehaviour
    {
        [Header("UI Prefabs (from FactorialFun package)")]
        [SerializeField] private GameObject _uiSystemPrefab;
        [SerializeField] private GameObject _eventSystemPrefab;

        private void Start()
        {
            EnsureUIInfrastructure();
            CreateSettingsButton();
            CreateSettingsPanel();
        }

        private void OnDestroy()
        {
            if (UIRoot.Instance != null)
            {
                UIRoot.Instance.Panels.Unregister<SettingsPanel>();
            }
        }

        private void EnsureUIInfrastructure()
        {
            if (UIRoot.Instance == null && _uiSystemPrefab != null)
            {
                Instantiate(_uiSystemPrefab);
            }

            if (_eventSystemPrefab != null && FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                Instantiate(_eventSystemPrefab);
            }
        }

        private void CreateSettingsButton()
        {
            // Settings button in the top-right corner, always visible.
            GameObject btnGo = new GameObject("SettingsButton", typeof(RectTransform));
            btnGo.transform.SetParent(UIRoot.Instance.ContentRoot, worldPositionStays: false);

            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 1f);
            btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.pivot = new Vector2(1f, 1f);
            btnRt.anchoredPosition = new Vector2(-16f, -16f);
            btnRt.sizeDelta = new Vector2(48f, 48f);

            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.45f, 0.9f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.2f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(ToggleSettings);

            // Gear icon placeholder (text).
            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(btnGo.transform, worldPositionStays: false);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            Text label = labelGo.AddComponent<Text>();
            label.text = "\u2699"; // gear unicode
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        private void CreateSettingsPanel()
        {
            GameObject panelGo = new GameObject("SettingsPanel", typeof(RectTransform));
            panelGo.transform.SetParent(UIRoot.Instance.ContentRoot, worldPositionStays: false);

            SettingsPanel panel = panelGo.AddComponent<SettingsPanel>();
            panel.OnSave += HandleSave;
            panel.OnClose += () => UIRoot.Instance.Hide<SettingsPanel>();

            UIRoot.Instance.Panels.Register(panel, priority: 10);
            // Panel starts hidden (PanelLayer.Register does not auto-hide, PanelBase starts active).
            panel.Hide();
        }

        private void ToggleSettings()
        {
            SettingsPanel panel = UIRoot.Instance.Panels.Get<SettingsPanel>();
            if (panel != null && panel.IsVisible)
            {
                UIRoot.Instance.Hide<SettingsPanel>();
            }
            else
            {
                UIRoot.Instance.Show<SettingsPanel>();
            }
        }

        private void HandleSave()
        {
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null)
            {
                Debug.LogError("[GameplayUISetup] GameBootstrapper not found — cannot save.");
                return;
            }

            GridService grid = bootstrapper.LogicWorld.Get<GridService>();
            WorldGenerationService worldGen = bootstrapper.LogicWorld.Get<WorldGenerationService>();

            SaveData saveData = new SaveData();
            saveData.Seed = worldGen.Seed;

            foreach (Vector3Int coord in grid.Grid.GetAllCoordinates())
            {
                Cell cell = grid.Grid.GetCell(coord);
                if (cell == null) { continue; }

                saveData.Cells.Add(new CellData
                {
                    X = coord.x,
                    Y = coord.y,
                    Z = coord.z,
                    TileTypeId = cell.TileId,
                });
            }

            string path = GameSession.SaveFilePath;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonUtility.ToJson(saveData, prettyPrint: true);
            File.WriteAllText(path, json);
            Debug.Log($"[GameplayUISetup] Saved game: {saveData.Cells.Count} cells, seed={saveData.Seed} → {path}");
        }
    }
}
