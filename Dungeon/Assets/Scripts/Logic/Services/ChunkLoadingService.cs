using System;
using System.Collections.Generic;
using Dungeon.Logic.Core;
using UnityEngine;

namespace Dungeon.Logic.Services
{
    [Serializable]
    public struct ChunkLoadingConfig
    {
        public int ChunkViewRadius;

        public static ChunkLoadingConfig Default => new ChunkLoadingConfig { ChunkViewRadius = 2 };
    }

    public class ChunkLoadingService : ILogicService
    {
        private readonly ChunkLoadingConfig _config;
        private LogicWorld _world;
        private GridService _grid;
        private WorldGenerationService _generation;

        // Set by CameraService each frame so chunk loading follows the camera.
        public Vector3 FocusPosition { get; set; }

        // Fired with the coordinates of every newly generated chunk at the current elevation.
        public event Action<IReadOnlyList<Vector2Int>> OnChunksLoaded;

        // (chunkX, elevation, chunkZ) — tracks generated data across all elevations.
        private readonly HashSet<Vector3Int> _loadedChunks = new();
        private Vector2Int _lastCenterChunk = new(int.MinValue, int.MinValue);

        public ChunkLoadingService(ChunkLoadingConfig config)
        {
            _config = config;
        }

        public void Initialize(LogicWorld world)
        {
            _world = world;
            _grid = world.Get<GridService>();
            _generation = world.Get<WorldGenerationService>();
        }

        public void Tick(float deltaTime)
        {
            Vector2Int center = _grid.WorldToChunk(FocusPosition);
            if (center == _lastCenterChunk) return;

            _lastCenterChunk = center;
            LoadChunksAround(center);
        }

        // Ensures chunk data exists around the current focus at the current elevation.
        // Called by ElevationService after changing elevation — does NOT fire OnChunksLoaded.
        public void EnsureChunksAtCurrentElevation()
        {
            Vector2Int center = _grid.WorldToChunk(FocusPosition);
            _lastCenterChunk = center;

            for (int cx = center.x - _config.ChunkViewRadius; cx <= center.x + _config.ChunkViewRadius; cx++)
            for (int cz = center.y - _config.ChunkViewRadius; cz <= center.y + _config.ChunkViewRadius; cz++)
            {
                var key = new Vector3Int(cx, _grid.Elevation, cz);
                if (_loadedChunks.Contains(key)) continue;

                GenerateChunk(new Vector2Int(cx, cz));
                _loadedChunks.Add(key);
            }
        }

        private void LoadChunksAround(Vector2Int center)
        {
            var newChunks = new List<Vector2Int>();

            for (int cx = center.x - _config.ChunkViewRadius; cx <= center.x + _config.ChunkViewRadius; cx++)
            for (int cz = center.y - _config.ChunkViewRadius; cz <= center.y + _config.ChunkViewRadius; cz++)
            {
                var key = new Vector3Int(cx, _grid.Elevation, cz);
                if (_loadedChunks.Contains(key)) continue;

                GenerateChunk(new Vector2Int(cx, cz));
                _loadedChunks.Add(key);
                newChunks.Add(new Vector2Int(cx, cz));
            }

            if (newChunks.Count > 0)
                OnChunksLoaded?.Invoke(newChunks);
        }

        private void GenerateChunk(Vector2Int chunkCoord)
        {
            CellGrid grid = _grid.Grid;
            int elevation = _grid.Elevation;
            int baseX = chunkCoord.x * WorldConstants.ChunkSize;
            int baseZ = chunkCoord.y * WorldConstants.ChunkSize;

            for (int lx = 0; lx < WorldConstants.ChunkSize; lx++)
            for (int lz = 0; lz < WorldConstants.ChunkSize; lz++)
            {
                int wx = baseX + lx;
                int wz = baseZ + lz;
                int tileId = _generation.GetTileId(wx, elevation, wz);
                grid.SetCell(new Vector3Int(wx, elevation, wz), new Cell(tileId));
            }
        }

        // Returns true if cell data has already been generated for the given chunk at the given elevation.
        public bool IsChunkLoaded(int chunkX, int elevation, int chunkZ)
        {
            return _loadedChunks.Contains(new Vector3Int(chunkX, elevation, chunkZ));
        }

        // Marks a chunk as loaded without generating it.
        // Used when populating the grid from a save file — the cells are already in the grid.
        public void MarkChunkLoaded(int chunkX, int elevation, int chunkZ)
        {
            _loadedChunks.Add(new Vector3Int(chunkX, elevation, chunkZ));
        }

        // Clears all chunk tracking state. Call before loading a save file.
        public void Reset()
        {
            _loadedChunks.Clear();
            _lastCenterChunk = new Vector2Int(int.MinValue, int.MinValue);
        }

        public int ChunkViewRadius => _config.ChunkViewRadius;

        public void Dispose() { }
    }
}
