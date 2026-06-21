#if UNITY_EDITOR
using Dungeon.Visuals;
using UnityEditor;
using UnityEngine;

namespace Dungeon.Editor
{
    [CustomEditor(typeof(LevelEditorTools))]
    public class LevelEditorToolsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LevelEditorTools tools = (LevelEditorTools)target;

            EditorGUILayout.Space(10);

            // ── Save / Load ──────────────────────────────────────────────
            EditorGUILayout.LabelField("Level", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Level", GUILayout.Height(30)))
                {
                    tools.SaveLevel();
                }
                if (GUILayout.Button("Load Level", GUILayout.Height(30)))
                {
                    tools.LoadLevel();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // ── Terrain Actions ──────────────────────────────────────────
            EditorGUILayout.LabelField("Terrain Actions", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Paint", GUILayout.Height(30)))
                {
                    tools.PaintTerrain();
                }
                if (GUILayout.Button("Erase", GUILayout.Height(30)))
                {
                    tools.EraseTerrain();
                }
                if (GUILayout.Button("Dig", GUILayout.Height(30)))
                {
                    tools.DigTerrain();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // ── World Object Actions ─────────────────────────────────────
            EditorGUILayout.LabelField("World Object Actions", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Place", GUILayout.Height(30)))
                {
                    tools.PlaceWorldObject();
                }
                if (GUILayout.Button("Remove", GUILayout.Height(30)))
                {
                    tools.RemoveWorldObject();
                }
                EditorGUILayout.EndHorizontal();
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to use these tools.", MessageType.Info);
            }
        }
    }
}
#endif
