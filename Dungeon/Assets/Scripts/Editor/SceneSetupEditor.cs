#if UNITY_EDITOR
using Dungeon.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dungeon.Editor
{
    // Editor menu items that create and configure game scenes programmatically.
    // Run these from the Unity menu bar: Dungeon > Create Main Menu Scene / Setup Gameplay UI.
    public static class SceneSetupEditor
    {
        private const string UI_SYSTEM_PATH = "Packages/com.factorialfun.shared/UI/Prefabs/UISystem.prefab";
        private const string EVENT_SYSTEM_PATH = "Packages/com.factorialfun.shared/UI/Prefabs/EventSystem.prefab";
        // ── Main Menu Scene ────────────────────────────────────────────────────────

        [MenuItem("Dungeon/Create Main Menu Scene")]
        public static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            // Camera.
            GameObject camGo = new GameObject("Main Camera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camGo.AddComponent<AudioListener>();

            // Add URP camera data if available.
            var urpDataType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpDataType != null)
            {
                camGo.AddComponent(urpDataType);
            }

            // Main menu setup controller.
            GameObject setupGo = new GameObject("MainMenuSetup");
            var setup = setupGo.AddComponent<MainMenuSetup>();

            // Assign prefab references via SerializedObject.
            GameObject uiSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI_SYSTEM_PATH);
            GameObject eventSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EVENT_SYSTEM_PATH);

            SerializedObject so = new SerializedObject(setup);
            so.FindProperty("_uiSystemPrefab").objectReferenceValue = uiSystemPrefab;
            so.FindProperty("_eventSystemPrefab").objectReferenceValue = eventSystemPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (uiSystemPrefab == null) { Debug.LogWarning("[SceneSetup] UISystem prefab not found at: " + UI_SYSTEM_PATH); }
            if (eventSystemPrefab == null) { Debug.LogWarning("[SceneSetup] EventSystem prefab not found at: " + EVENT_SYSTEM_PATH); }

            // Save scene.
            string scenePath = "Assets/Scenes/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[SceneSetup] Created Main Menu scene at {scenePath}. Remember to add it to Build Settings.");
        }

        // ── Gameplay UI Setup ──────────────────────────────────────────────────────

        [MenuItem("Dungeon/Setup Gameplay Scene UI")]
        public static void SetupGameplayUI()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            // Check if GameplayUISetup already exists.
            if (Object.FindAnyObjectByType<GameplayUISetup>() != null)
            {
                Debug.LogWarning("[SceneSetup] GameplayUISetup already exists in the active scene.");
                return;
            }

            GameObject setupGo = new GameObject("GameplayUISetup");
            var setup = setupGo.AddComponent<GameplayUISetup>();

            // Assign prefab references.
            GameObject uiSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI_SYSTEM_PATH);
            GameObject eventSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EVENT_SYSTEM_PATH);

            SerializedObject so = new SerializedObject(setup);
            so.FindProperty("_uiSystemPrefab").objectReferenceValue = uiSystemPrefab;
            so.FindProperty("_eventSystemPrefab").objectReferenceValue = eventSystemPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (uiSystemPrefab == null) { Debug.LogWarning("[SceneSetup] UISystem prefab not found at: " + UI_SYSTEM_PATH); }
            if (eventSystemPrefab == null) { Debug.LogWarning("[SceneSetup] EventSystem prefab not found at: " + EVENT_SYSTEM_PATH); }

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log($"[SceneSetup] Added GameplayUISetup to '{activeScene.name}'. Save the scene to keep changes.");
        }
    }
}
#endif
