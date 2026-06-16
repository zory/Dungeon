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
    }

    public class WorldRenderService : IVisualService
    {
        private readonly WorldRenderConfig _config;
        private readonly Transform _chunkParent;
        private VisualWorld _world;
        private GridService _grid;
        private ChunkLoadingService _chunkLoader;

        private readonly Dictionary<Vector2Int, DualGridChunkRenderer> _chunks = new();

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
            _chunkLoader.OnChunksLoaded += OnChunksLoaded;
        }

        public void Tick(float deltaTime) { }

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

        // Destroys all chunk GameObjects and respawns only chunks that have cells
        // at the currently active elevation layer.
        public void RebuildAll()
        {
            int elevation = _grid.Elevation;

            var occupiedChunks = new HashSet<Vector2Int>();
            foreach (Vector3Int coord in _grid.Grid.GetAllCoordinates())
            {
                if (coord.y == elevation)
                    occupiedChunks.Add(GridService.CellToChunk(coord.x, coord.z));
            }

            ClearChunks();

            foreach (Vector2Int chunkCoord in occupiedChunks)
                SpawnChunk(chunkCoord);
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
