using Dungeon.Visuals.Services;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    public class WorldRenderAuthoring : MonoBehaviour
    {
        [SerializeField] private DualGridAtlas _atlas;
        [SerializeField] private TileColorRegistry _colorRegistry;
        [SerializeField] private Material _material;
        [SerializeField] private int _chunkUnloadRadius = 4;

        // Chunks will be parented under this transform.
        public Transform ChunkParent => transform;

        public WorldRenderConfig GetConfig() => new WorldRenderConfig
        {
            Atlas = _atlas,
            ColorRegistry = _colorRegistry,
            Material = _material,
            ChunkUnloadRadius = _chunkUnloadRadius,
        };
    }
}
