using System.Collections.Generic;
using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Renders chunks as they are generated using the dual-grid autotile system.
    // Listens to WorldGenerator.OnWorldGenerated (finite/pre-built worlds)
    // and ChunkLoader.OnChunksLoaded (infinite streaming) — both can be active at once.
    public class WorldRenderer : MonoBehaviour
    {
        [SerializeField] private GridManager    _gridManager;
        [SerializeField] private GridRenderer   _gridRenderer;
        [SerializeField] private WorldGenerator _worldGenerator; // optional: finite pre-built world
        [SerializeField] private ChunkLoader    _chunkLoader;    // optional: infinite streaming

        [Header("Dual-Grid Autotile")]
        [SerializeField] private DualGridAtlas     _atlas;
        [SerializeField] private TileColorRegistry _colorRegistry;
        [SerializeField] private Material          _material; // DualGridTile shader

        private readonly Dictionary<Vector2Int, DualGridChunkRenderer> _chunks = new();

        private void OnEnable()
        {
            if (_worldGenerator != null)
                _worldGenerator.OnWorldGenerated += OnWorldGenerated;

            if (_chunkLoader != null)
                _chunkLoader.OnChunksLoaded += OnChunksLoaded;
        }

        private void OnDisable()
        {
            if (_worldGenerator != null)
                _worldGenerator.OnWorldGenerated -= OnWorldGenerated;

            if (_chunkLoader != null)
                _chunkLoader.OnChunksLoaded -= OnChunksLoaded;
        }

        private void OnWorldGenerated()
        {
            ClearChunks();

            var occupied = new HashSet<Vector2Int>();
            foreach (var coord in _gridManager.Grid.GetAllCoordinates())
                occupied.Add(CellToChunk(coord.x, coord.z));

            foreach (var chunkCoord in occupied)
                SpawnChunk(chunkCoord);
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
                chunk.Build(chunkCoord, _gridManager, _gridRenderer, _atlas, _colorRegistry,
                            _material);
        }

        private void SpawnChunk(Vector2Int chunkCoord)
        {
            if (_chunks.ContainsKey(chunkCoord)) return;

            float cs            = _gridRenderer.CellSize;
            float chunkWorldLen = DualGridChunkRenderer.ChunkSize * cs;

            var go = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.position = new Vector3(
                chunkCoord.x * chunkWorldLen + _gridRenderer.XZOffset.x,
                _gridRenderer.WorldY,
                chunkCoord.y * chunkWorldLen + _gridRenderer.XZOffset.y);

            var chunk = go.AddComponent<DualGridChunkRenderer>();
            chunk.Build(chunkCoord, _gridManager, _gridRenderer, _atlas, _colorRegistry,
                        _material);

            _chunks[chunkCoord] = chunk;
        }

        // Rebuilds a single chunk's meshes from the current grid state.
        // Called by EditorToolController after painting or erasing a cell.
        public void RebuildChunk(Vector2Int chunkCoord)
        {
            if (_chunks.TryGetValue(chunkCoord, out var existing))
                existing.Build(chunkCoord, _gridManager, _gridRenderer, _atlas, _colorRegistry,
                               _material);
            else
                SpawnChunk(chunkCoord);
        }

        // Destroys all chunk GameObjects and respawns only chunks that have cells
        // at the currently active elevation layer.
        public void RebuildAll()
        {
            int elevation = _gridRenderer.ElevationLayer;

            var occupiedChunks = new HashSet<Vector2Int>();
            foreach (Vector3Int coord in _gridManager.Grid.GetAllCoordinates())
            {
                if (coord.y == elevation)
                    occupiedChunks.Add(CellToChunk(coord.x, coord.z));
            }

            ClearChunks();

            foreach (Vector2Int chunkCoord in occupiedChunks)
                SpawnChunk(chunkCoord);
        }

        private void ClearChunks()
        {
            foreach (var chunk in _chunks.Values)
                if (chunk != null) Destroy(chunk.gameObject);
            _chunks.Clear();
        }

        private static Vector2Int CellToChunk(int cellX, int cellZ) => new Vector2Int(
            Mathf.FloorToInt((float)cellX / DualGridChunkRenderer.ChunkSize),
            Mathf.FloorToInt((float)cellZ / DualGridChunkRenderer.ChunkSize));
    }
}
