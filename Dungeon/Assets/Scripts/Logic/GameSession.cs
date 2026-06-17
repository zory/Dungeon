using Dungeon.Logic.Serialisation;
using UnityEngine;

namespace Dungeon.Logic
{
    // Static cross-scene state used to pass intent from the main menu to the gameplay scene.
    public static class GameSession
    {
        public enum StartMode { NewGame, LoadGame }

        public static StartMode Mode = StartMode.NewGame;

        // Populated by the main menu when the player chooses Load Game.
        // Consumed once by GameBootstrapper on scene load, then cleared.
        public static SaveData LoadedSaveData;

        public const string SAVE_FILENAME = "save.json";

        public static string SaveFilePath => Application.persistentDataPath + "/" + SAVE_FILENAME;
    }
}
