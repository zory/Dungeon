using System;
using System.Collections.Generic;
using Dungeon.Logic.Core;
using UnityEngine;

namespace Dungeon.Logic.Services
{
    public class WorldObjectService : ILogicService
    {
        private readonly Dictionary<int, WorldObject> _objects = new();
        private readonly Dictionary<Vector3Int, int> _cellToObjectId = new();
        private int _nextId;

        public IReadOnlyDictionary<int, WorldObject> All => _objects;

        public void Initialize(LogicWorld world) { }

        public void Tick(float deltaTime) { }

        // Assigns an ID and registers the object. Returns the assigned ID.
        public int Register(WorldObject obj)
        {
            if (obj == null) { throw new ArgumentNullException(nameof(obj)); }
            int id = ++_nextId;
            obj.SetId(id);
            _objects[id] = obj;
            return id;
        }

        // Register the cells this object occupies (from Footprint).
        // One cell can only hold one object — overwrites any previous occupant.
        public void OccupyCells(WorldObject obj)
        {
            if (!obj.TryGetFeature<Footprint>(out Footprint footprint)) { return; }

            List<Vector3Int> worldCells = footprint.GetWorldCells(obj.CellCoords);
            for (int i = 0; i < worldCells.Count; i++)
            {
                _cellToObjectId[worldCells[i]] = obj.Id;
            }
        }

        // Remove this object's cells from the spatial index.
        public void VacateCells(WorldObject obj)
        {
            if (!obj.TryGetFeature<Footprint>(out Footprint footprint)) { return; }

            List<Vector3Int> worldCells = footprint.GetWorldCells(obj.CellCoords);
            for (int i = 0; i < worldCells.Count; i++)
            {
                if (_cellToObjectId.TryGetValue(worldCells[i], out int occupant) && occupant == obj.Id)
                {
                    _cellToObjectId.Remove(worldCells[i]);
                }
            }
        }

        // Returns the object occupying the given cell, or null.
        public WorldObject GetObjectAtCell(Vector3Int cell)
        {
            if (_cellToObjectId.TryGetValue(cell, out int id))
            {
                return Get(id);
            }
            return null;
        }

        // Returns the object ID at the given cell, or -1 if empty.
        public int GetObjectIdAtCell(Vector3Int cell)
        {
            return _cellToObjectId.TryGetValue(cell, out int id) ? id : -1;
        }

        public WorldObject Get(int id) =>
            _objects.TryGetValue(id, out var o) ? o : null;

        public bool TryGet(int id, out WorldObject obj) =>
            _objects.TryGetValue(id, out obj);

        public void Remove(int id)
        {
            if (_objects.TryGetValue(id, out WorldObject obj))
            {
                VacateCells(obj);
            }
            _objects.Remove(id);
        }

        public void Clear()
        {
            _objects.Clear();
            _cellToObjectId.Clear();
            _nextId = 0;
        }

        public void Dispose() => Clear();
    }
}
