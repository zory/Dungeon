using Dungeon.Logic.Services;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    public class ChunkLoadingAuthoring : MonoBehaviour
    {
        [SerializeField] private int _chunkViewRadius = 2;

        public ChunkLoadingConfig GetConfig() => new ChunkLoadingConfig
        {
            ChunkViewRadius = _chunkViewRadius,
        };
    }
}
