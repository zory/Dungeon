using Dungeon.Logic.Services;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    public class GridAuthoring : MonoBehaviour
    {
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private Vector2 _xzOffset = Vector2.zero;

        public GridConfig GetConfig() => new GridConfig
        {
            CellSize = _cellSize,
            XZOffset = _xzOffset,
        };
    }
}
