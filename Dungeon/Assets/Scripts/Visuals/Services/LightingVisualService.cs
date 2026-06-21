using System.Collections.Generic;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using Dungeon.Visuals.Lighting;
using UnityEngine;

namespace Dungeon.Visuals.Services
{
    // Visual service: syncs scene MonoBehaviours (LightSource, ShadowCaster2DCustom)
    // to Logic WorldObject features so the LightingService can query them.
    // Sibling SpriteShadowCasters (same parent Transform) are grouped into a single
    // shadow caster to prevent siblings from casting shadows on each other.
    public class LightingVisualService : IVisualService
    {
        private WorldObjectService _objects;
        private GridService _grid;

        // Tracked MonoBehaviour -> WorldObject mappings.
        private readonly List<TrackedLight> _trackedLights = new();
        private readonly List<TrackedCaster> _trackedCasters = new();
        private readonly List<TrackedSpriteCasterGroup> _trackedSpriteCasterGroups = new();

        // Reusable list to avoid per-frame allocations when combining sibling paths.
        private readonly List<Vector2[]> _tempPathList = new();

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

        // A group of sibling SpriteShadowCasters that share a parent Transform.
        // They are merged into a single Logic WorldObject so they cannot shadow each other.
        private struct TrackedSpriteCasterGroup
        {
            public List<SpriteShadowCaster> Casters;
            public SpriteHeightProfile HeightProfile;
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

            // Scan scene for existing SpriteShadowCasters, grouped by parent Transform.
            // Siblings under the same parent are combined into one shadow caster so they
            // cannot cast shadows on each other (they are parts of the same logical object).
            SpriteShadowCaster[] spriteCasters = Object.FindObjectsByType<SpriteShadowCaster>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Dictionary<Transform, List<SpriteShadowCaster>> groups = new();
            foreach (SpriteShadowCaster caster in spriteCasters)
            {
                Transform groupKey = caster.transform.parent != null ? caster.transform.parent : caster.transform;
                if (!groups.TryGetValue(groupKey, out List<SpriteShadowCaster> list))
                {
                    list = new List<SpriteShadowCaster>();
                    groups[groupKey] = list;
                }
                list.Add(caster);
            }
            foreach (KeyValuePair<Transform, List<SpriteShadowCaster>> kvp in groups)
            {
                RegisterSpriteShadowCasterGroup(kvp.Value);
            }
        }

        private void RegisterLightSource(LightSource mono)
        {
            Vector3 worldPos = mono.transform.position;
            var obj = new Logic.WorldObject("LightSource", worldPos);
            obj.SetPosition(worldPos, _grid.CellSize, _grid.XZOffset, _grid.Elevation);

            var feature = new Logic.LightSource(mono.Radius, mono.Offset, mono.Enabled, mono.Height);
            obj.AddFeature(feature);

            _objects.Register(obj);
            _trackedLights.Add(new TrackedLight { MonoBehaviour = mono, ObjectId = obj.Id });
        }

        private void RegisterShadowCaster(ShadowCaster2DCustom mono)
        {
            Vector3 worldPos = mono.transform.position;
            var obj = new Logic.WorldObject("ShadowCaster", worldPos);
            obj.SetPosition(worldPos, _grid.CellSize, _grid.XZOffset, _grid.Elevation);

            Vector2[][] localPaths = mono.GetLocalPaths();
            var feature = new Logic.ShadowCaster(localPaths);
            obj.AddFeature(feature);

            // Create a default HeightProfile from the caster's Height value.
            var hpFeature = new Logic.HeightProfile(0f, mono.Height, 0f, 1f);
            obj.AddFeature(hpFeature);

            _objects.Register(obj);
            _trackedCasters.Add(new TrackedCaster { MonoBehaviour = mono, ObjectId = obj.Id });
        }

        private void RegisterSpriteShadowCasterGroup(List<SpriteShadowCaster> casters)
        {
            if (casters == null || casters.Count == 0) { return; }

            // Use the first caster's position as the group position.
            Vector3 worldPos = casters[0].transform.position;
            var obj = new Logic.WorldObject("SpriteShadowCaster", worldPos);
            obj.SetPosition(worldPos, _grid.CellSize, _grid.XZOffset, _grid.Elevation);

            // Combine shadow paths from all casters in the group.
            _tempPathList.Clear();
            foreach (SpriteShadowCaster caster in casters)
            {
                Vector2[][] localPaths = caster.GetLocalPaths();
                if (localPaths == null) { continue; }
                foreach (Vector2[] path in localPaths)
                {
                    if (path != null && path.Length >= 3)
                    {
                        _tempPathList.Add(path);
                    }
                }
            }

            var feature = new Logic.ShadowCaster(_tempPathList.ToArray());
            feature.SkipOccluder = true;
            obj.AddFeature(feature);

            // Find HeightProfile from any sibling with a SpriteHeightProfile component.
            SpriteHeightProfile heightProfile = null;
            foreach (SpriteShadowCaster caster in casters)
            {
                heightProfile = caster.GetComponent<SpriteHeightProfile>();
                if (heightProfile != null) { break; }
            }

            if (heightProfile != null)
            {
                var hpFeature = new Logic.HeightProfile(
                    heightProfile.GroundOffset,
                    heightProfile.TotalHeight,
                    heightProfile.GetNormalizedHeightStart(),
                    heightProfile.GetNormalizedHeightEnd());
                obj.AddFeature(hpFeature);
            }

            _objects.Register(obj);
            _trackedSpriteCasterGroups.Add(new TrackedSpriteCasterGroup
            {
                Casters = casters,
                HeightProfile = heightProfile,
                ObjectId = obj.Id
            });
        }

        public void Tick(float deltaTime)
        {
            // Sync MonoBehaviour state -> Logic features each frame.
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
                feature.Height = tracked.MonoBehaviour.Height;
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
                feature.LocalPaths = tracked.MonoBehaviour.GetLocalPaths();
                feature.Enabled = tracked.MonoBehaviour.CastsShadows && tracked.MonoBehaviour.gameObject.activeInHierarchy;

                // Sync height from the component.
                if (obj.TryGetFeature<Logic.HeightProfile>(out Logic.HeightProfile hpFeature))
                {
                    hpFeature.TotalHeight = tracked.MonoBehaviour.Height;
                }
            }

            foreach (TrackedSpriteCasterGroup tracked in _trackedSpriteCasterGroups)
            {
                if (tracked.Casters == null || tracked.Casters.Count == 0) { continue; }
                if (!_objects.TryGet(tracked.ObjectId, out Logic.WorldObject obj)) { continue; }
                if (!obj.TryGetFeature<Logic.ShadowCaster>(out Logic.ShadowCaster feature)) { continue; }

                // Update position from first living caster.
                SpriteShadowCaster firstCaster = null;
                foreach (SpriteShadowCaster c in tracked.Casters)
                {
                    if (c != null) { firstCaster = c; break; }
                }
                if (firstCaster == null) { continue; }

                Vector3 worldPos = firstCaster.transform.position;
                obj.SetPosition(worldPos, _grid.CellSize, _grid.XZOffset, _grid.Elevation);

                // Re-combine shadow paths from all living casters.
                _tempPathList.Clear();
                bool anyEnabled = false;
                foreach (SpriteShadowCaster caster in tracked.Casters)
                {
                    if (caster == null) { continue; }
                    if (caster.CastsShadows && caster.gameObject.activeInHierarchy)
                    {
                        anyEnabled = true;
                    }
                    Vector2[][] paths = caster.GetLocalPaths();
                    if (paths == null) { continue; }
                    foreach (Vector2[] path in paths)
                    {
                        if (path != null && path.Length >= 3)
                        {
                            _tempPathList.Add(path);
                        }
                    }
                }
                feature.LocalPaths = _tempPathList.ToArray();
                feature.Enabled = anyEnabled;

                // Sync height profile if present.
                if (tracked.HeightProfile != null && obj.TryGetFeature<Logic.HeightProfile>(out Logic.HeightProfile hpFeature))
                {
                    hpFeature.GroundOffset = tracked.HeightProfile.GroundOffset;
                    hpFeature.TotalHeight = tracked.HeightProfile.TotalHeight;
                    hpFeature.HeightStart = tracked.HeightProfile.GetNormalizedHeightStart();
                    hpFeature.HeightEnd = tracked.HeightProfile.GetNormalizedHeightEnd();
                }
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

            foreach (TrackedSpriteCasterGroup tracked in _trackedSpriteCasterGroups)
            {
                _objects.Remove(tracked.ObjectId);
            }
            _trackedSpriteCasterGroups.Clear();
        }
    }
}
