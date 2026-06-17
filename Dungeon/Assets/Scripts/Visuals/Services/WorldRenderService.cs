using System;
using System.Collections.Generic;
using Dungeon.Logic;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using UnityEngine;

namespace Dungeon.Visuals.Services
{
    [Serializable]
    public struct WorldRenderConfig
    {
        public DualGridAtlas Atlas;
        public TileColorRegistry ColorRegistry;
        public Material Material;
        public int ChunkUnloadRadius;

        public static WorldRenderConfig Default => new WorldRenderConfig { ChunkUnloadRadius = 4 };
    }

    public class WorldRenderService : IVisualService
    {
        private readonly WorldRenderConfig _config;
        private readonly Transform _chunkParent;
        private VisualWorld _world;
        private GridService _grid;
        private ChunkLoadingService _chunkLoader;
        private CameraService _camera;

        private readonly Dictionary<Vector2Int, DualGridChunkRenderer> _chunks = new();
        private Vector2Int _lastRenderCenter = new(int.MinValue, int.MinValue);

        public WorldRenderService(WorldRenderConfig config, Transform chunkParent)
        {
            _config = config;
            _chunkParent = chunkParent;
        }

        public void Initialize(VisualWorld world)
        {
            _world = world;
            _grid = world.GetLogic<GridService>();
            _chunkLoader = world.GetLogic<ChunkLoadingService>();
            _camera = world.Get<CameraService>();
            _chunkLoader.OnChunksLoaded += OnChunksLoaded;
        }

        public void Tick(float deltaTime)
        {
            Vector2Int center = _grid.WorldToChunk(_camera.Camera.transform.position);
            if (center == _lastRenderCenter) { return; }
            _lastRenderCenter = center;

            int viewRadius = _chunkLoader.ChunkViewRadius;
            int unloadRadius = _config.ChunkUnloadRadius > 0 ? _config.ChunkUnloadRadius : viewRadius + 2;

            // Unload renderers beyond unload radius.
            var toRemove = new List<Vector2Int>();
            foreach (Vector2Int key in _chunks.Keys)
            {
                int dx = Mathf.Abs(key.x - center.x);
                int dz = Mathf.Abs(key.y - center.y);
                if (dx > unloadRadius || dz > unloadRadius)
                {
                    toRemove.Add(key);
                }
            }
            foreach (Vector2Int key in toRemove)
            {
                if (_chunks.TryGetValue(key, out DualGridChunkRenderer chunk) && chunk != null)
                {
                    UnityEngine.Object.Destroy(chunk.gameObject);
                }
                _chunks.Remove(key);
            }

            // Re-render chunks within view radius that have data but no renderer.
            int elevation = _grid.Elevation;
            for (int cx = center.x - viewRadius; cx <= center.x + viewRadius; cx++)
            for (int cz = center.y - viewRadius; cz <= center.y + viewRadius; cz++)
            {
                Vector2Int coord = new Vector2Int(cx, cz);
                if (!_chunks.ContainsKey(coord) && _chunkLoader.IsChunkLoaded(cx, elevation, cz))
                {
                    SpawnChunk(coord);
                }
            }
        }

        private void OnChunksLoaded(IReadOnlyList<Vector2Int> newChunks)
        {
            foreach (var chunkCoord in newChunks)
                SpawnChunk(chunkCoord);

            // Edge tiles of existing neighbors sampled None for cells now populated in new chunks.
            // Rebuild those neighbors so their edges render correctly.
            foreach (var chunkCoord in newChunks)
            {
                TryRebuildExisting(new Vector2Int(chunkCoord.x - 1, chunkCoord.y    ));
                TryRebuildExisting(new Vector2Int(chunkCoord.x,     chunkCoord.y - 1));
                TryRebuildExisting(new Vector2Int(chunkCoord.x - 1, chunkCoord.y - 1));
            }
        }

        private void TryRebuildExisting(Vector2Int chunkCoord)
        {
            if (_chunks.TryGetValue(chunkCoord, out var chunk))
                chunk.Build(chunkCoord, _grid.Grid, _grid.Elevation, _grid.CellSize,
                            _config.Atlas, _config.ColorRegistry, _config.Material);
        }

        private void SpawnChunk(Vector2Int chunkCoord)
        {
            if (_chunks.ContainsKey(chunkCoord)) return;

            float cs = _grid.CellSize;
            float chunkWorldLen = DualGridChunkRenderer.ChunkSize * cs;

            var go = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
            go.transform.SetParent(_chunkParent, worldPositionStays: false);
            go.transform.position = new Vector3(
                chunkCoord.x * chunkWorldLen + _grid.XZOffset.x,
                _grid.WorldY,
                chunkCoord.y * chunkWorldLen + _grid.XZOffset.y);

            var chunk = go.AddComponent<DualGridChunkRenderer>();
            chunk.Build(chunkCoord, _grid.Grid, _grid.Elevation, _grid.CellSize,
                        _config.Atlas, _config.ColorRegistry, _config.Material);

            _chunks[chunkCoord] = chunk;
        }

        // Rebuilds a single chunk's meshes from the current grid state.
        public void RebuildChunk(Vector2Int chunkCoord)
        {
            if (_chunks.TryGetValue(chunkCoord, out var existing))
                existing.Build(chunkCoord, _grid.Grid, _grid.Elevation, _grid.CellSize,
                               _config.Atlas, _config.ColorRegistry, _config.Material);
            else
                SpawnChunk(chunkCoord);
        }

        // Clears all renderers, then spawns only chunks around camera at current elevation.
        // O(viewRadius²) — independent of total world size.
        public void RebuildForCurrentView()
        {
            ClearChunks();

            Vector2Int center = _grid.WorldToChunk(_camera.Camera.transform.position);
            _lastRenderCenter = center;
            int viewRadius = _chunkLoader.ChunkViewRadius;
            int elevation = _grid.Elevation;

            for (int cx = center.x - viewRadius; cx <= center.x + viewRadius; cx++)
            for (int cz = center.y - viewRadius; cz <= center.y + viewRadius; cz++)
            {
                if (_chunkLoader.IsChunkLoaded(cx, elevation, cz))
                {
                    SpawnChunk(new Vector2Int(cx, cz));
                }
            }
        }

        private void ClearChunks()
        {
            foreach (var chunk in _chunks.Values)
                if (chunk != null) UnityEngine.Object.Destroy(chunk.gameObject);
            _chunks.Clear();
        }

        public void Dispose()
        {
            if (_chunkLoader != null)
                _chunkLoader.OnChunksLoaded -= OnChunksLoaded;
            ClearChunks();
        }
    }
}
