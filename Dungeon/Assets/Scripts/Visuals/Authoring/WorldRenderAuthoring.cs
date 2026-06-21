using Dungeon.Visuals.Services;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    public class WorldRenderAuthoring : MonoBehaviour
    {
        [SerializeField] private TerrainAtlas _terrainAtlas;
        [SerializeField] private Material _material;
        [SerializeField] private int _chunkUnloadRadius = 4;
        [SerializeField] private WorldObjectDatabase _worldObjectDatabase;

        // Chunks will be parented under this transform.
        public Transform ChunkParent => transform;

        public TerrainAtlas TerrainAtlas => _terrainAtlas;
        public WorldObjectDatabase WorldObjectDatabase => _worldObjectDatabase;

        public WorldRenderConfig GetConfig() => new WorldRenderConfig
        {
            TerrainAtlas = _terrainAtlas,
            Material = _material,
            ChunkUnloadRadius = _chunkUnloadRadius,
        };
    }
}
