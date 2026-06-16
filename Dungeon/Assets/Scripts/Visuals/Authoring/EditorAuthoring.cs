using Dungeon.Visuals.Services;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    public class EditorAuthoring : MonoBehaviour
    {
        [SerializeField] private string _savePath = "Assets/Levels/level.json";
        [SerializeField] private ObjectDefinitionRegistry _registry;

        public EditorConfig GetConfig() => new EditorConfig
        {
            SavePath = _savePath,
            Registry = _registry,
        };
    }
}
