using System;
using System.Collections.Generic;
using Dungeon.Logic.Core;

namespace Dungeon.Logic.Services
{
    public class WorldObjectService : ILogicService
    {
        private readonly Dictionary<int, WorldObject> _objects = new();
        private int _nextId;

        public IReadOnlyDictionary<int, WorldObject> All => _objects;

        public void Initialize(LogicWorld world) { }

        public void Tick(float deltaTime) { }

        // Assigns an ID and registers the object. Returns the assigned ID.
        public int Register(WorldObject obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            int id = ++_nextId;
            obj.SetId(id);
            _objects[id] = obj;
            return id;
        }

        public WorldObject Get(int id) =>
            _objects.TryGetValue(id, out var o) ? o : null;

        public bool TryGet(int id, out WorldObject obj) =>
            _objects.TryGetValue(id, out obj);

        public void Remove(int id) => _objects.Remove(id);

        public void Clear()
        {
            _objects.Clear();
            _nextId = 0;
        }

        public void Dispose() => Clear();
    }
}
