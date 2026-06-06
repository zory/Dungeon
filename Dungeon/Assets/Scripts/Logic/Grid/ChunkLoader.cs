using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Logic
{
    // Tracks which chunks are loaded and generates new ones as the view target moves.
    // Each time new chunks are generated the OnChunksLoaded event fires with their coords
    // so the visual layer can build meshes for exactly those chunks.
    //
    // Note: _cellSize, _xzOffset, and _elevation must match the GridRenderer settings.
    public class ChunkLoader : MonoBehaviour
    {
        [SerializeField] private GridManager    _gridManager;
        [SerializeField] private WorldDataSource _dataSource;
        [SerializeField] private Transform       _viewTarget;   // usually the camera transform

        [Header("View Radius")]
        [SerializeField] private int _chunkViewRadius = 3;     // chunks to keep loaded around the target

        [Header("Grid Settings — must match GridRenderer")]
        [SerializeField] private float   _cellSize  = 1f;
        [SerializeField] private Vector2 _xzOffset  = Vector2.zero;
        [SerializeField] private int     _elevation  = 0;

        // Fired with the coordinates of every newly generated chunk.
        public event Action<IReadOnlyList<Vector2Int>> OnChunksLoaded;

        private readonly HashSet<Vector2Int> _loadedChunks = new();
        private Vector2Int _lastCenterChunk = new(int.MinValue, int.MinValue);

        private void Update()
        {
            if (_viewTarget == null) return;

            Vector2Int center = WorldToChunk(_viewTarget.position);
            if (center == _lastCenterChunk) return;

            _lastCenterChunk = center;
            LoadChunksAround(center);
        }

        private void LoadChunksAround(Vector2Int center)
        {
            var newChunks = new List<Vector2Int>();

            for (int cx = center.x - _chunkViewRadius; cx <= center.x + _chunkViewRadius; cx++)
            for (int cz = center.y - _chunkViewRadius; cz <= center.y + _chunkViewRadius; cz++)
            {
                var coord = new Vector2Int(cx, cz);
                if (_loadedChunks.Contains(coord)) continue;

                GenerateChunk(coord);
                _loadedChunks.Add(coord);
                newChunks.Add(coord);
            }

            if (newChunks.Count > 0)
                OnChunksLoaded?.Invoke(newChunks);
        }

        private void GenerateChunk(Vector2Int chunkCoord)
        {
            var grid  = _gridManager.Grid;
            int baseX = chunkCoord.x * WorldConstants.ChunkSize;
            int baseZ = chunkCoord.y * WorldConstants.ChunkSize;

            for (int lx = 0; lx < WorldConstants.ChunkSize; lx++)
            for (int lz = 0; lz < WorldConstants.ChunkSize; lz++)
            {
                int wx = baseX + lx;
                int wz = baseZ + lz;
                int tileId = _dataSource != null ? _dataSource.GetTileId(wx, _elevation, wz) : 0;
                grid.SetCell(new Vector3Int(wx, _elevation, wz), new Cell(tileId));
            }
        }

        private Vector2Int WorldToChunk(Vector3 worldPos)
        {
            float chunkWorldSize = _cellSize * WorldConstants.ChunkSize;
            return new Vector2Int(
                Mathf.FloorToInt((worldPos.x - _xzOffset.x) / chunkWorldSize),
                Mathf.FloorToInt((worldPos.z - _xzOffset.y) / chunkWorldSize));
        }
    }
}
