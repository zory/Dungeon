using System.Collections.Generic;
using Dungeon.Logic;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using UnityEngine;

namespace Dungeon.Visuals.Services
{
    // Visual service: manages wall chunk renderers for obstacle autotiling.
    //
    // Listens to ObstacleService events to track which cells contain walls
    // (obstacles with ObstacleTypeId > 0), then rebuilds affected wall chunks.
    // Only shows walls at the current elevation — rebuilds on elevation change.
    //
    // Each chunk gets its own WallChunkRenderer with a mesh built from
    // cardinal-neighbor bitmasks (N=1, E=2, S=4, W=8).
    public class WallRenderService : IVisualService
    {
        private readonly TerrainAtlas _terrainAtlas;
        private readonly Material _wallMaterial;
        private readonly Transform _chunkParent;

        private GridService _grid;
        private ObstacleService _obstacles;
        private WorldObjectService _objects;
        private CameraService _camera;
        private ChunkLoadingService _chunkLoader;

        // All wall cells across all elevations: cell coord → obstacle type ID.
        private readonly Dictionary<Vector3Int, int> _wallCells = new();

        // Object ID → list of wall cell coords (for removal).
        private readonly Dictionary<int, List<Vector3Int>> _objectWallCells = new();

        // Active chunk renderers (current elevation only).
        private readonly Dictionary<Vector2Int, WallChunkRenderer> _chunks = new();

        private int _lastElevation = int.MinValue;
        private Vector2Int _lastRenderCenter = new(int.MinValue, int.MinValue);

        public WallRenderService(TerrainAtlas terrainAtlas, Material wallMaterial, Transform chunkParent)
        {
            _terrainAtlas = terrainAtlas;
            _wallMaterial = wallMaterial;
            _chunkParent = chunkParent;
        }

        public void Initialize(VisualWorld world)
        {
            _grid = world.GetLogic<GridService>();
            _obstacles = world.GetLogic<ObstacleService>();
            _objects = world.GetLogic<WorldObjectService>();
            _camera = world.Get<CameraService>();
            _chunkLoader = world.GetLogic<ChunkLoadingService>();

            _obstacles.OnObstacleRegistered += HandleObstacleRegistered;
            _obstacles.OnObstacleUnregistered += HandleObstacleUnregistered;

            // Scan already-registered obstacles.
            foreach (KeyValuePair<int, WorldObject> kvp in _objects.All)
            {
                WorldObject obj = kvp.Value;
                if (obj.TryGetFeature<Obstacle>(out Obstacle obstacle) && obstacle.ObstacleTypeId > 0)
                {
                    TrackObstacle(obj, obstacle);
                }
            }
        }

        public void Tick(float deltaTime)
        {
            int currentElevation = _grid.Elevation;

            // On elevation change, clear all chunks and rebuild.
            if (currentElevation != _lastElevation)
            {
                _lastElevation = currentElevation;
                _lastRenderCenter = new Vector2Int(int.MinValue, int.MinValue);
                ClearChunks();
            }

            // Track camera position for chunk loading/unloading.
            Vector2Int center = _grid.WorldToChunk(_camera.Camera.transform.position);
            if (center == _lastRenderCenter) { return; }
            _lastRenderCenter = center;

            int viewRadius = _chunkLoader.ChunkViewRadius;

            // Unload chunks beyond view radius.
            List<Vector2Int> toRemove = new List<Vector2Int>();
            foreach (Vector2Int key in _chunks.Keys)
            {
                int dx = Mathf.Abs(key.x - center.x);
                int dz = Mathf.Abs(key.y - center.y);
                if (dx > viewRadius + 2 || dz > viewRadius + 2)
                {
                    toRemove.Add(key);
                }
            }
            foreach (Vector2Int key in toRemove)
            {
                if (_chunks.TryGetValue(key, out WallChunkRenderer chunk) && chunk != null)
                {
                    Object.Destroy(chunk.gameObject);
                }
                _chunks.Remove(key);
            }

            // Spawn/rebuild chunks within view radius.
            for (int cx = center.x - viewRadius; cx <= center.x + viewRadius; cx++)
            for (int cz = center.y - viewRadius; cz <= center.y + viewRadius; cz++)
            {
                Vector2Int coord = new Vector2Int(cx, cz);
                if (!_chunks.ContainsKey(coord))
                {
                    SpawnChunk(coord);
                }
            }
        }

        private void HandleObstacleRegistered(WorldObject obj)
        {
            if (!obj.TryGetFeature<Obstacle>(out Obstacle obstacle)) { return; }
            if (obstacle.ObstacleTypeId <= 0) { return; }

            TrackObstacle(obj, obstacle);

            // Rebuild affected chunks if at current elevation.
            if (obj.CellCoords.y == _lastElevation)
            {
                RebuildAffectedChunks(obj.CellCoords);
            }
        }

        private void HandleObstacleUnregistered(int objectId)
        {
            if (!_objectWallCells.TryGetValue(objectId, out List<Vector3Int> cells)) { return; }

            foreach (Vector3Int cell in cells)
            {
                _wallCells.Remove(cell);
            }
            _objectWallCells.Remove(objectId);

            // Rebuild affected chunks if any cell was at current elevation.
            foreach (Vector3Int cell in cells)
            {
                if (cell.y == _lastElevation)
                {
                    RebuildAffectedChunks(cell);
                }
            }
        }

        private void TrackObstacle(WorldObject obj, Obstacle obstacle)
        {
            List<Vector3Int> blockedCells = obstacle.GetBlockedWorldCells(obj.CellCoords);
            List<Vector3Int> trackedCells = new List<Vector3Int>(blockedCells.Count);

            foreach (Vector3Int cell in blockedCells)
            {
                _wallCells[cell] = obstacle.ObstacleTypeId;
                trackedCells.Add(cell);
            }

            _objectWallCells[obj.Id] = trackedCells;
        }

        // Rebuild the chunk containing the given cell and its cardinal neighbors
        // (for bitmask correctness at chunk boundaries).
        private void RebuildAffectedChunks(Vector3Int cell)
        {
            HashSet<Vector2Int> affected = new HashSet<Vector2Int>();
            Vector2Int chunkCoord = GridService.CellToChunk(cell.x, cell.z);
            affected.Add(chunkCoord);
            affected.Add(new Vector2Int(chunkCoord.x - 1, chunkCoord.y));
            affected.Add(new Vector2Int(chunkCoord.x + 1, chunkCoord.y));
            affected.Add(new Vector2Int(chunkCoord.x, chunkCoord.y - 1));
            affected.Add(new Vector2Int(chunkCoord.x, chunkCoord.y + 1));

            foreach (Vector2Int coord in affected)
            {
                if (_chunks.TryGetValue(coord, out WallChunkRenderer chunk))
                {
                    chunk.Build(coord, _lastElevation, _grid.CellSize, _terrainAtlas, _wallMaterial, _wallCells);
                }
            }
        }

        private void SpawnChunk(Vector2Int chunkCoord)
        {
            if (_chunks.ContainsKey(chunkCoord)) { return; }

            float cs = _grid.CellSize;
            float chunkWorldLen = WallChunkRenderer.ChunkSize * cs;

            GameObject go = new GameObject($"WallChunk_{chunkCoord.x}_{chunkCoord.y}");
            go.transform.SetParent(_chunkParent, worldPositionStays: false);
            go.transform.position = new Vector3(
                chunkCoord.x * chunkWorldLen + _grid.XZOffset.x,
                _grid.WorldY,
                chunkCoord.y * chunkWorldLen + _grid.XZOffset.y);

            WallChunkRenderer chunk = go.AddComponent<WallChunkRenderer>();
            chunk.Build(chunkCoord, _lastElevation, _grid.CellSize, _terrainAtlas, _wallMaterial, _wallCells);

            _chunks[chunkCoord] = chunk;
        }

        public void RebuildForCurrentView()
        {
            ClearChunks();
            _lastRenderCenter = new Vector2Int(int.MinValue, int.MinValue);
        }

        private void ClearChunks()
        {
            foreach (WallChunkRenderer chunk in _chunks.Values)
            {
                if (chunk != null) { Object.Destroy(chunk.gameObject); }
            }
            _chunks.Clear();
        }

        public void Dispose()
        {
            if (_obstacles != null)
            {
                _obstacles.OnObstacleRegistered -= HandleObstacleRegistered;
                _obstacles.OnObstacleUnregistered -= HandleObstacleUnregistered;
            }

            ClearChunks();
            _wallCells.Clear();
            _objectWallCells.Clear();
        }
    }
}
