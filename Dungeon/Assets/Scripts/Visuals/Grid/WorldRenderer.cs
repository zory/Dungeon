using System.Collections.Generic;
using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Listens for WorldGenerator.OnWorldGenerated and spawns one ChunkRenderer
    // per occupied chunk in the Grid.
    public class WorldRenderer : MonoBehaviour
    {
        [SerializeField] private GridManager    _gridManager;
        [SerializeField] private GridRenderer   _gridRenderer;
        [SerializeField] private WorldGenerator _worldGenerator;

        [Header("Sprite Sheet")]
        [SerializeField] private Material      _material;
        [SerializeField] private TileRegistry  _tileRegistry;
        [SerializeField] private int _sheetColumns = 8;
        [SerializeField] private int _sheetRows    = 8;

        private readonly Dictionary<Vector2Int, ChunkRenderer> _chunks = new();
        private bool _built;

        private void OnEnable()
        {
            if (_worldGenerator != null)
                _worldGenerator.OnWorldGenerated += RebuildAll;
        }

        private void OnDisable()
        {
            if (_worldGenerator != null)
                _worldGenerator.OnWorldGenerated -= RebuildAll;
        }

        private void Start()
        {
            // Fallback: if WorldGenerator already fired before we subscribed, build now.
            if (!_built && _gridManager.Grid.Count > 0)
                RebuildAll();
        }

        private void RebuildAll()
        {
            _built = true;
            ClearChunks();

            var occupied = new HashSet<Vector2Int>();
            foreach (var coord in _gridManager.Grid.GetAllCoordinates())
                occupied.Add(CellToChunk(coord.x, coord.z));

            foreach (var chunkCoord in occupied)
                SpawnChunk(chunkCoord);
        }

        private void SpawnChunk(Vector2Int chunkCoord)
        {
            float cs            = _gridRenderer.CellSize;
            float chunkWorldLen = ChunkRenderer.ChunkSize * cs;

            var go = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.position = new Vector3(
                chunkCoord.x * chunkWorldLen + _gridRenderer.XZOffset.x,
                _gridRenderer.WorldY,
                chunkCoord.y * chunkWorldLen + _gridRenderer.XZOffset.y);

            var chunk = go.AddComponent<ChunkRenderer>();
            go.GetComponent<MeshRenderer>().sharedMaterial = _material;
            chunk.Build(chunkCoord, _gridManager, _gridRenderer, _tileRegistry, _sheetColumns, _sheetRows);

            _chunks[chunkCoord] = chunk;
        }

        private void ClearChunks()
        {
            foreach (var chunk in _chunks.Values)
                if (chunk != null) Destroy(chunk.gameObject);
            _chunks.Clear();
        }

        private static Vector2Int CellToChunk(int cellX, int cellZ) => new Vector2Int(
            Mathf.FloorToInt((float)cellX / ChunkRenderer.ChunkSize),
            Mathf.FloorToInt((float)cellZ / ChunkRenderer.ChunkSize));
    }
}
