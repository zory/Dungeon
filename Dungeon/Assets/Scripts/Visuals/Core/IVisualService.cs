using System;

namespace Dungeon.Visuals.Core
{
    public interface IVisualService : IDisposable
    {
        void Initialize(VisualWorld world);
        void Tick(float deltaTime);
    }
}
