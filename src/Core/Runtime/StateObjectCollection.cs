using System;
using System.Collections.Immutable;
using System.Linq;

namespace HotChocolate.Runtime
{
    public class StateObjectCollection<TKey>
        : IDisposable
    {
        private readonly object _sync = new object();
        private bool _disposed;

        private ImmutableDictionary<TKey, object> _instances =
            ImmutableDictionary<TKey, object>.Empty;

        public StateObjectCollection(ExecutionScope scope)
        {
            Scope = scope;
        }

        public ExecutionScope Scope { get; }

        public bool TryGetObject(
            TKey key,
            out object instance)
        {
            return _instances.TryGetValue(key, out instance);
        }

        public object CreateObject(
            IScopedStateDescriptor<TKey> descriptor,
            Func<object> serviceFactory)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (serviceFactory == null)
            {
                throw new ArgumentNullException(nameof(serviceFactory));
            }

            if (!_instances.TryGetValue(descriptor.Key, out object instance))
            {
                lock (_sync)
                {
                    if (!_instances.TryGetValue(descriptor.Key, out instance))
                    {
                        instance = serviceFactory();
                        _instances = _instances
                            .SetItem(descriptor.Key, instance);
                    }
                }
            }

            return instance;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                foreach (IDisposable disposable in
                    _instances.Values.OfType<IDisposable>())
                {
                    disposable.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
