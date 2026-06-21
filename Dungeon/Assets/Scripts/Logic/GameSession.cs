using Dungeon.Logic.Serialisation;
using UnityEngine;

namespace Dungeon.Logic
{
    // ScriptableObject singleton for cross-scene state (main menu → gameplay).
    // Lives as an asset; accessed via GameSession.Instance.
    // Testable: create an instance with ScriptableObject.CreateInstance<GameSession>().
    public class GameSession : ScriptableObject
    {
        public enum StartMode { NewGame, LoadGame }

        private static GameSession _instance;

        public static GameSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameSession>("GameSession");
                    if (_instance == null)
                    {
                        // Fallback for tests and first-time setup.
                        _instance = CreateInstance<GameSession>();
                    }
                }
                return _instance;
            }
        }

        [HideInInspector] public StartMode Mode = StartMode.NewGame;

        // Populated by the main menu when the player chooses Load Game.
        // Consumed once by GameBootstrapper on scene load, then cleared.
        [System.NonSerialized] public SaveData LoadedSaveData;

        public const string SAVE_FILENAME = "save.json";

        public string SaveFilePath => Application.persistentDataPath + "/" + SAVE_FILENAME;
    }
}
