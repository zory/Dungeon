using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Central database of all placeable world objects.
    // The absolute base — every system that spawns or references world objects
    // should look them up here. Supports lookup by int Id (inspector tools)
    // and by string TypeId (serialization).
    // Create via: Assets → Create → Dungeon → World Object Database
    [CreateAssetMenu(fileName = "WorldObjectDatabase", menuName = "Dungeon/World Object Database")]
    public class WorldObjectDatabase : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public int Id;
            [Tooltip("Unique string identifier used in level serialization.")]
            public string TypeId;
            public string Name;
            public string Category;
            public GameObject Prefab;
        }

        [SerializeField] private List<Entry> _entries = new();

        private Dictionary<int, Entry> _lookupById;
        private Dictionary<string, Entry> _lookupByTypeId;

        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            _lookupById = new Dictionary<int, Entry>(_entries.Count);
            _lookupByTypeId = new Dictionary<string, Entry>(_entries.Count);
            foreach (Entry entry in _entries)
            {
                if (entry.Id > 0)
                {
                    _lookupById[entry.Id] = entry;
                }
                if (!string.IsNullOrEmpty(entry.TypeId))
                {
                    _lookupByTypeId[entry.TypeId] = entry;
                }
            }
        }

        public bool TryGet(int id, out Entry entry)
        {
            if (_lookupById == null) { BuildLookup(); }
            return _lookupById.TryGetValue(id, out entry);
        }

        public bool TryGetByTypeId(string typeId, out Entry entry)
        {
            if (_lookupByTypeId == null) { BuildLookup(); }
            return _lookupByTypeId.TryGetValue(typeId, out entry);
        }

        public IReadOnlyList<Entry> AllEntries => _entries;
    }
}
