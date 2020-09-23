using System;
using System.Collections.Concurrent;
using System.Linq;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing
{
    internal sealed class DefaultActivator : IActivator
    {
        private readonly ConcurrentDictionary<Type, object?> _instances =
            new ConcurrentDictionary<Type, object?>();
        private readonly IServiceProvider _services;

        public DefaultActivator(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public T CreateInstance<T>() =>
            ActivatorHelper.CompileFactory<T>()(_services);

        public object? CreateInstance(Type type) =>
            ActivatorHelper.CompileFactory(type)(_services);

        public T GetOrCreate<T>() => (T)GetOrCreate(typeof(T))!;

        public object? GetOrCreate(Type type) =>
            _instances.GetOrAdd(type, CreateInstance);

        public void Dispose()
        {
            if(_instances.Count > 0) 
            {
                foreach (object? instance in _instances.Values.ToArray())
                {
                    if (instance is IDisposable d)
                    {
                        d.Dispose();
                    }
                }
                _instances.Clear();
            }
        }
    }
}
