using System;
using System.Collections.Concurrent;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing
{
    internal sealed class DefaultActivator : IActivator
    {
        private readonly ConcurrentDictionary<Type, Service> _instances = new();

        public T CreateInstance<T>(IServiceProvider services) =>
            ActivatorHelper.CompileFactory<T>()(services);

        public object? CreateInstance(Type type, IServiceProvider services) =>
            ActivatorHelper.CompileFactory(type)(services);

        public T GetOrCreate<T>(IServiceProvider services) =>
            (T)GetOrCreate(typeof(T), services)!;

        public object? GetOrCreate(Type type, IServiceProvider services) =>
            _instances.GetOrAdd(type, _ => new Service(this, type)).GetOrCreateService(services);

        public void Dispose()
        {
            if(_instances.Count > 0)
            {
                foreach (Service service in _instances.Values)
                {
                    service.Dispose();
                }
                _instances.Clear();
            }
        }

        private class Service : IDisposable
        {
            private readonly DefaultActivator _activator;
            private readonly Type _type;
            private object? _value;
            private bool _disposed;

            public Service(DefaultActivator activator, Type type)
            {
                _activator = activator;
                _type = type;
            }

            public object? GetOrCreateService(IServiceProvider services)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(typeof(Service).FullName);
                }

                if (_value is not null)
                {
                    return _value;
                }

                if (_type != typeof(object))
                {
                    services.TryGetService(_type, out object? value);

                    if (value is null && !_type.IsAbstract && !_type.IsInterface)
                    {
                        value = _activator.CreateInstance(_type, services);
                        _value = value;
                    }

                    return value;
                }

                return null;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    if (_value is IDisposable d)
                    {
                        d.Dispose();
                    }
                    _disposed = true;
                }
            }
        }
    }
}
