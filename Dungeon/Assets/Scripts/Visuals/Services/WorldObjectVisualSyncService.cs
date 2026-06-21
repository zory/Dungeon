using System.Collections.Generic;
using Dungeon.Logic;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using UnityEngine;

namespace Dungeon.Visuals.Services
{
    // Visual service: syncs transforms for ALL WorldObjects that have the Mover feature.
    // Handles characters, pushable crates, projectiles, dynamic obstacles, etc.
    // CharacterService handles sprite direction only — position sync lives here.
    public class WorldObjectVisualSyncService : IVisualService
    {
        private WorldObjectService _objects;

        // Maps object ID → root transform of the scene GameObject.
        private readonly Dictionary<int, Transform> _trackedTransforms = new();

        public void Initialize(VisualWorld world)
        {
            _objects = world.GetLogic<WorldObjectService>();
        }

        // Called by GameBootstrapper after registering a WorldObject that has a Mover feature.
        public void Track(int objectId, Transform rootTransform)
        {
            _trackedTransforms[objectId] = rootTransform;
        }

        public void Untrack(int objectId)
        {
            _trackedTransforms.Remove(objectId);
        }

        public void Tick(float deltaTime)
        {
            foreach (KeyValuePair<int, Transform> kvp in _trackedTransforms)
            {
                if (!_objects.TryGet(kvp.Key, out WorldObject obj)) { continue; }
                if (kvp.Value == null) { continue; }

                Vector3 logicPos = obj.WorldPosition;
                Transform t = kvp.Value;
                t.position = new Vector3(logicPos.x, t.position.y, logicPos.z);
            }
        }

        public void Dispose()
        {
            _trackedTransforms.Clear();
        }
    }
}
