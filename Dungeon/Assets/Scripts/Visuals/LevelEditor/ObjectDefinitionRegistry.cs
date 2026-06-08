using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // ScriptableObject mapping typeId strings to prefabs.
    // Used by EditorToolController (placement) and LevelLoader (runtime spawning).
    // Create via: Assets → Create → Dungeon → Object Definition Registry
    [CreateAssetMenu(fileName = "ObjectDefinitionRegistry", menuName = "Dungeon/Object Definition Registry")]
    public class ObjectDefinitionRegistry : ScriptableObject
    {
        [Serializable]
        public class ObjectDefinition
        {
            public string     TypeId;
            public GameObject Prefab;
        }

        [SerializeField] private List<ObjectDefinition> _definitions = new List<ObjectDefinition>();

        private Dictionary<string, ObjectDefinition> _lookup;

        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, ObjectDefinition>(_definitions.Count);
            foreach (ObjectDefinition def in _definitions)
            {
                if (!string.IsNullOrEmpty(def.TypeId))
                    _lookup[def.TypeId] = def;
            }
        }

        public bool TryGet(string typeId, out ObjectDefinition definition)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(typeId, out definition);
        }

        public IReadOnlyList<ObjectDefinition> AllDefinitions => _definitions;
    }
}
