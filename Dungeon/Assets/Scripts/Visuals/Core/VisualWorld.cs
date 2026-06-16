using System;
using System.Collections.Generic;
using Dungeon.Logic.Core;

namespace Dungeon.Visuals.Core
{
    public class VisualWorld
    {
        private readonly LogicWorld _logicWorld;
        private readonly List<IVisualService> _services = new();
        private readonly Dictionary<Type, IVisualService> _serviceMap = new();

        public VisualWorld(LogicWorld logicWorld)
        {
            _logicWorld = logicWorld;
        }

        public void Register<T>(T service) where T : class, IVisualService
        {
            _services.Add(service);
            _serviceMap[typeof(T)] = service;
        }

        public T Get<T>() where T : class, IVisualService
        {
            if (_serviceMap.TryGetValue(typeof(T), out var service))
                return (T)service;
            throw new InvalidOperationException($"[VisualWorld] Service {typeof(T).Name} not registered.");
        }

        public bool TryGet<T>(out T service) where T : class, IVisualService
        {
            if (_serviceMap.TryGetValue(typeof(T), out var s))
            {
                service = (T)s;
                return true;
            }
            service = null;
            return false;
        }

        public T GetLogic<T>() where T : class, ILogicService => _logicWorld.Get<T>();

        public void InitializeAll()
        {
            for (int i = 0; i < _services.Count; i++)
                _services[i].Initialize(this);
        }

        public void TickAll(float deltaTime)
        {
            for (int i = 0; i < _services.Count; i++)
                _services[i].Tick(deltaTime);
        }

        public void DisposeAll()
        {
            for (int i = _services.Count - 1; i >= 0; i--)
                _services[i].Dispose();
            _services.Clear();
            _serviceMap.Clear();
        }
    }
}
