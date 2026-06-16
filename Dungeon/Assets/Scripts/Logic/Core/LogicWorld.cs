using System;
using System.Collections.Generic;

namespace Dungeon.Logic.Core
{
    public class LogicWorld
    {
        private readonly List<ILogicService> _services = new();
        private readonly Dictionary<Type, ILogicService> _serviceMap = new();

        public void Register<T>(T service) where T : class, ILogicService
        {
            _services.Add(service);
            _serviceMap[typeof(T)] = service;
        }

        public T Get<T>() where T : class, ILogicService
        {
            if (_serviceMap.TryGetValue(typeof(T), out var service))
                return (T)service;
            throw new InvalidOperationException($"[LogicWorld] Service {typeof(T).Name} not registered.");
        }

        public bool TryGet<T>(out T service) where T : class, ILogicService
        {
            if (_serviceMap.TryGetValue(typeof(T), out var s))
            {
                service = (T)s;
                return true;
            }
            service = null;
            return false;
        }

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
