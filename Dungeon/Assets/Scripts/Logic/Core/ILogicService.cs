using System;

namespace Dungeon.Logic.Core
{
    public interface ILogicService : IDisposable
    {
        void Initialize(LogicWorld world);
        void Tick(float deltaTime);
    }
}
