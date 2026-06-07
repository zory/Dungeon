using System;
using System.Collections.Generic;

namespace Dungeon.Logic
{
    // Global store for all active WorldObjects, keyed by ID.
    // Logic owns the objects; MonoBehaviours hold IDs as lightweight handles.
    public static class WorldObjectRegistry
    {
        private static readonly Dictionary<int, WorldObject> _objects = new();

        public static IReadOnlyDictionary<int, WorldObject> All => _objects;

        public static void Register(WorldObject obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (_objects.ContainsKey(obj.Id))
                throw new InvalidOperationException($"[WorldObjectRegistry] Object {obj.Id} already registered.");
            _objects[obj.Id] = obj;
        }

        public static bool TryGet(int id, out WorldObject obj) =>
            _objects.TryGetValue(id, out obj);

        public static WorldObject Get(int id) =>
            _objects.TryGetValue(id, out var o) ? o : null;

        public static void Remove(int id) => _objects.Remove(id);

        public static void Clear() => _objects.Clear();
    }
}
