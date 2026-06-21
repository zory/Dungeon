using System.IO;
using Dungeon.Logic;
using Dungeon.Logic.Serialisation;
using Dungeon.UI.Panels;
using FactorialFun.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dungeon.UI
{
    // Placed in the MainMenu scene. Bootstraps UI infrastructure and shows the main menu.
    public class MainMenuSetup : MonoBehaviour
    {
        [Header("UI Prefabs (from FactorialFun package)")]
        [SerializeField] private GameObject _uiSystemPrefab;
        [SerializeField] private GameObject _eventSystemPrefab;

        [Header("Scene")]
        [SerializeField] private string _gameplaySceneName = "SampleScene";

        private void Start()
        {
            EnsureUIInfrastructure();

            // Create and register the main menu panel under UIRoot's content root.
            GameObject panelGo = new GameObject("MainMenuPanel", typeof(RectTransform));
            panelGo.transform.SetParent(UIRoot.Instance.ContentRoot, worldPositionStays: false);

            MainMenuPanel panel = panelGo.AddComponent<MainMenuPanel>();
            panel.OnNewGame += HandleNewGame;
            panel.OnLoadGame += HandleLoadGame;
            panel.OnExit += HandleExit;

            UIRoot.Instance.Panels.Register(panel);
            UIRoot.Instance.Show<MainMenuPanel>();
        }

        private void OnDestroy()
        {
            if (UIRoot.Instance != null)
            {
                UIRoot.Instance.Panels.Unregister<MainMenuPanel>();
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

        private void HandleNewGame()
        {
            GameSession.Instance.Mode = GameSession.StartMode.NewGame;
            GameSession.Instance.LoadedSaveData = null;
            SceneManager.LoadScene(_gameplaySceneName);
        }

        private void HandleLoadGame()
        {
            string path = GameSession.Instance.SaveFilePath;
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[MainMenuSetup] No save file found at: {path}");
                return;
            }

            string json = File.ReadAllText(path);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);
            if (saveData == null)
            {
                Debug.LogError($"[MainMenuSetup] Failed to parse save file: {path}");
                return;
            }

            GameSession.Instance.Mode = GameSession.StartMode.LoadGame;
            GameSession.Instance.LoadedSaveData = saveData;
            SceneManager.LoadScene(_gameplaySceneName);
        }

        private void HandleExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
