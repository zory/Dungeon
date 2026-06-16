using Dungeon.Visuals.Services;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    public class GridRenderAuthoring : MonoBehaviour
    {
        [SerializeField] private int _renderRadius = 30;
        [SerializeField] private Color _lineColor = new Color(1f, 1f, 1f, 0.5f);

        public GridRenderConfig GetConfig() => new GridRenderConfig
        {
            RenderRadius = _renderRadius,
            LineColor = _lineColor,
        };
    }
}
