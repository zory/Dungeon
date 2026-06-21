using System.Collections.Generic;
using Dungeon.Logic;
using Dungeon.Logic.Services;
using Dungeon.Visuals;
using Dungeon.Visuals.Core;
using UnityEngine;

namespace Dungeon.Visuals.Services
{
    // Visual service: creates visuals for obstacle WorldObjects that have
    // ObstacleTypeId > 0 (programmatically created obstacles like underground walls).
    // Obstacles placed via authoring already have scene visuals and use ObstacleTypeId=0.
    //
    // Uses WorldObjectDatabase for obstacle prefab lookup. For now, all obstacles
    // are rendered as colored sprites tinted by the terrain type colour from
    // TerrainAtlas. When proper obstacle prefabs are added to WorldObjectDatabase,
    // this service will instantiate those instead.
    public class ObstacleVisualService : IVisualService
    {
        private readonly TerrainAtlas _terrainAtlas;
        private readonly WorldObjectDatabase _worldObjectDatabase;
        private Sprite _sprite;

        private GridService _grid;
        private ObstacleService _obstacles;
        private WorldObjectService _objects;

        private GameObject _root;
        private Texture2D _runtimeTexture;
        private readonly Dictionary<int, GameObject> _visuals = new();

        // Tracks the elevation each obstacle visual belongs to, for elevation-based visibility.
        private readonly Dictionary<int, int> _visualElevations = new();
        private int _lastElevation = int.MinValue;

        public ObstacleVisualService(TerrainAtlas terrainAtlas, WorldObjectDatabase worldObjectDatabase)
        {
            _terrainAtlas = terrainAtlas;
            _worldObjectDatabase = worldObjectDatabase;
        }

        private Sprite GetOrCreateSprite()
        {
            if (_sprite != null) { return _sprite; }

            // Create a 1x1 white texture sprite at runtime.
            _runtimeTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _runtimeTexture.SetPixel(0, 0, Color.white);
            _runtimeTexture.Apply();
            _runtimeTexture.filterMode = FilterMode.Point;
            _sprite = Sprite.Create(_runtimeTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _sprite;
        }

        public void Initialize(VisualWorld world)
        {
            _grid = world.GetLogic<GridService>();
            _obstacles = world.GetLogic<ObstacleService>();
            _objects = world.GetLogic<WorldObjectService>();

            _root = new GameObject("ObstacleVisuals");

            _obstacles.OnObstacleRegistered += HandleObstacleRegistered;
            _obstacles.OnObstacleUnregistered += HandleObstacleUnregistered;
        }

        public void Tick(float deltaTime)
        {
            int currentElevation = _grid.Elevation;
            if (currentElevation != _lastElevation)
            {
                _lastElevation = currentElevation;
                SyncVisibility();
            }
        }

        private void SyncVisibility()
        {
            foreach (KeyValuePair<int, GameObject> kvp in _visuals)
            {
                if (_visualElevations.TryGetValue(kvp.Key, out int elevation))
                {
                    kvp.Value.SetActive(elevation == _lastElevation);
                }
            }
        }

        private void HandleObstacleRegistered(WorldObject obj)
        {
            if (!obj.TryGetFeature<Obstacle>(out Obstacle obstacle)) { return; }
            if (obstacle.ObstacleTypeId <= 0) { return; }
            if (_visuals.ContainsKey(obj.Id)) { return; }

            CreateVisual(obj, obstacle);
        }

        private void HandleObstacleUnregistered(int objectId)
        {
            if (!_visuals.TryGetValue(objectId, out GameObject go)) { return; }

            Object.Destroy(go);
            _visuals.Remove(objectId);
            _visualElevations.Remove(objectId);
        }

        private void CreateVisual(WorldObject obj, Obstacle obstacle)
        {
            Vector3 pos = _grid.CellCenter(obj.CellCoords);
            Color color = _terrainAtlas.GetColor(obstacle.ObstacleTypeId);
            Sprite sprite = GetOrCreateSprite();

            var go = new GameObject($"Obstacle_{obj.Id}_{obstacle.ObstacleTypeId}");
            go.transform.SetParent(_root.transform, worldPositionStays: false);
            go.transform.position = pos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 5;

            // Scale sprite to fill one cell.
            float cellSize = _grid.CellSize;
            float ppu = sprite.pixelsPerUnit;
            float sw = sprite.rect.width / ppu;
            float sh = sprite.rect.height / ppu;
            go.transform.localScale = new Vector3(cellSize / sw, cellSize / sh, 1f);

            // Only show obstacle if it belongs to the currently viewed elevation.
            int obstacleElevation = obj.CellCoords.y;
            go.SetActive(obstacleElevation == _grid.Elevation);

            _visuals[obj.Id] = go;
            _visualElevations[obj.Id] = obstacleElevation;
        }

        // Creates a visual for an obstacle that already exists (e.g. debug tool adding visuals retroactively).
        public void TrackExistingObstacle(int objectId)
        {
            if (_visuals.ContainsKey(objectId)) { return; }
            if (!_objects.TryGet(objectId, out WorldObject obj)) { return; }
            if (!obj.TryGetFeature<Obstacle>(out Obstacle obstacle)) { return; }
            if (obstacle.ObstacleTypeId <= 0) { return; }

            CreateVisual(obj, obstacle);
        }

        public void Dispose()
        {
            if (_obstacles != null)
            {
                _obstacles.OnObstacleRegistered -= HandleObstacleRegistered;
                _obstacles.OnObstacleUnregistered -= HandleObstacleUnregistered;
            }

            foreach (KeyValuePair<int, GameObject> kvp in _visuals)
            {
                if (kvp.Value != null)
                {
                    Object.Destroy(kvp.Value);
                }
            }
            _visuals.Clear();
            _visualElevations.Clear();

            if (_runtimeTexture != null)
            {
                Object.Destroy(_runtimeTexture);
            }

            if (_root != null)
            {
                Object.Destroy(_root);
            }
        }
    }
}
