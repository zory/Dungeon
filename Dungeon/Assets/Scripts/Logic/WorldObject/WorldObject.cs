using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Logic
{
    // Base concept for every entity in the world — characters, trees, items, buildings, etc.
    // Features are added via composition; nothing is inherited.
    // IDs are assigned by WorldObjectService.Register(), not internally.
    public class WorldObject
    {
        private readonly Dictionary<Type, object> _features = new();

        public int        Id            { get; private set; }
        public string     Name          { get; set; }
        public Vector3    WorldPosition { get; private set; }
        public Vector3Int CellCoords    { get; private set; }

        public WorldObject(string name, Vector3 position)
        {
            Name          = name;
            WorldPosition = position;
        }

        // Called by WorldObjectService.Register() to assign the ID.
        public void SetId(int id) => Id = id;

        // ── Feature composition ────────────────────────────────────────────────────────
        public void AddFeature<T>(T feature) where T : class =>
            _features[typeof(T)] = feature;

        public T GetFeature<T>() where T : class =>
            _features.TryGetValue(typeof(T), out var f) ? (T)f : null;

        public bool HasFeature<T>() where T : class =>
            _features.ContainsKey(typeof(T));

        public bool TryGetFeature<T>(out T feature) where T : class
        {
            if (_features.TryGetValue(typeof(T), out var f)) { feature = (T)f; return true; }
            feature = null;
            return false;
        }

        // ── Position ───────────────────────────────────────────────────────────────────
        // Called by Locomotion (and any other feature that needs to move this object).
        public void SetPosition(Vector3 position, float cellSize, Vector2 xzOffset, int elevation)
        {
            WorldPosition = position;
            CellCoords    = ComputeCell(position, cellSize, xzOffset, elevation);
        }

        private static Vector3Int ComputeCell(Vector3 pos, float cellSize, Vector2 xzOffset, int elevation) =>
            new Vector3Int(
                Mathf.FloorToInt((pos.x - xzOffset.x) / cellSize),
                elevation,
                Mathf.FloorToInt((pos.z - xzOffset.y) / cellSize));
    }
}
