using System.Collections.Generic;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using Dungeon.Visuals.Lighting;
using UnityEngine;

namespace Dungeon.Visuals.Services
{
    // Visual service: syncs scene MonoBehaviours (LightSource, ShadowCaster2DCustom)
    // to Logic WorldObject features so the LightingService can query them.
    public class LightingVisualService : IVisualService
    {
        private WorldObjectService _objects;
        private GridService _grid;

        // Tracked MonoBehaviour → WorldObject mappings.
        private readonly List<TrackedLight> _trackedLights = new();
        private readonly List<TrackedCaster> _trackedCasters = new();

        private struct TrackedLight
        {
            public LightSource MonoBehaviour;
            public int ObjectId;
        }

        private struct TrackedCaster
        {
            public ShadowCaster2DCustom MonoBehaviour;
            public int ObjectId;
        }

        public void Initialize(VisualWorld world)
        {
            _objects = world.GetLogic<WorldObjectService>();
            _grid = world.GetLogic<GridService>();

            // Scan scene for existing LightSource MonoBehaviours.
            LightSource[] sceneLights = Object.FindObjectsByType<LightSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (LightSource light in sceneLights)
            {
                RegisterLightSource(light);
            }

            // Scan scene for existing ShadowCaster2DCustom MonoBehaviours.
            ShadowCaster2DCustom[] sceneCasters = Object.FindObjectsByType<ShadowCaster2DCustom>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (ShadowCaster2DCustom caster in sceneCasters)
            {
                RegisterShadowCaster(caster);
            }
        }

        private void RegisterLightSource(LightSource mono)
        {
            Vector3 worldPos = mono.transform.position;
            var obj = new Logic.WorldObject("LightSource", worldPos);
            obj.SetPosition(worldPos, _grid.CellSize, _grid.XZOffset, _grid.Elevation);

            var feature = new Logic.LightSource(mono.Radius, mono.Offset, mono.Enabled);
            obj.AddFeature(feature);

            _objects.Register(obj);
            _trackedLights.Add(new TrackedLight { MonoBehaviour = mono, ObjectId = obj.Id });
        }

        private void RegisterShadowCaster(ShadowCaster2DCustom mono)
        {
            Vector3 worldPos = mono.transform.position;
            var obj = new Logic.WorldObject("ShadowCaster", worldPos);
            obj.SetPosition(worldPos, _grid.CellSize, _grid.XZOffset, _grid.Elevation);

            // Use the MonoBehaviour's GetWorldPoints-style local points.
            Vector2[] localPoints = mono.GetLocalPoints();
            var feature = new Logic.ShadowCaster(localPoints, mono.MaxShadowLength);
            obj.AddFeature(feature);

            _objects.Register(obj);
            _trackedCasters.Add(new TrackedCaster { MonoBehaviour = mono, ObjectId = obj.Id });
        }

        public void Tick(float deltaTime)
        {
            // Sync MonoBehaviour state → Logic features each frame.
            foreach (TrackedLight tracked in _trackedLights)
            {
                if (tracked.MonoBehaviour == null) { continue; }
                if (!_objects.TryGet(tracked.ObjectId, out Logic.WorldObject obj)) { continue; }
                if (!obj.TryGetFeature<Logic.LightSource>(out Logic.LightSource feature)) { continue; }

                // Update position.
                Vector3 worldPos = tracked.MonoBehaviour.transform.position;
                obj.SetPosition(worldPos, _grid.CellSize, _grid.XZOffset, _grid.Elevation);

                // Update feature data.
                feature.Radius = tracked.MonoBehaviour.Radius;
                feature.Offset = tracked.MonoBehaviour.Offset;
                feature.Enabled = tracked.MonoBehaviour.Enabled;
            }

            foreach (TrackedCaster tracked in _trackedCasters)
            {
                if (tracked.MonoBehaviour == null) { continue; }
                if (!_objects.TryGet(tracked.ObjectId, out Logic.WorldObject obj)) { continue; }
                if (!obj.TryGetFeature<Logic.ShadowCaster>(out Logic.ShadowCaster feature)) { continue; }

                // Update position.
                Vector3 worldPos = tracked.MonoBehaviour.transform.position;
                obj.SetPosition(worldPos, _grid.CellSize, _grid.XZOffset, _grid.Elevation);

                // Update feature data.
                feature.LocalPoints = tracked.MonoBehaviour.GetLocalPoints();
                feature.MaxShadowLength = tracked.MonoBehaviour.MaxShadowLength;
            }
        }

        public void Dispose()
        {
            foreach (TrackedLight tracked in _trackedLights)
            {
                _objects.Remove(tracked.ObjectId);
            }
            _trackedLights.Clear();

            foreach (TrackedCaster tracked in _trackedCasters)
            {
                _objects.Remove(tracked.ObjectId);
            }
            _trackedCasters.Clear();
        }
    }
}
