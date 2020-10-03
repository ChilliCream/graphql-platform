using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching.Client
{
    public class StitchingContext
        : IStitchingContext
    {
        private readonly object _sync = new object();
        private readonly List<IObserver<IRemoteRequestExecutor>> _observers =
            new List<IObserver<IRemoteRequestExecutor>>();
        private readonly IDictionary<NameString, RemoteRequestExecutor> _clients;

        public StitchingContext(
            IServiceProvider services,
            IEnumerable<IRemoteExecutorAccessor> executors)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (executors == null)
            {
                throw new ArgumentNullException(nameof(executors));
            }

            _clients = executors.ToDictionary(
                t => t.SchemaName,
                t => new RemoteRequestExecutor(services, t.Executor));
        }

        public IRemoteRequestExecutor GetRemoteQueryClient(NameString schemaName)
        {
            schemaName.EnsureNotEmpty(nameof(schemaName));

            if (_clients.TryGetValue(schemaName, out RemoteRequestExecutor client))
            {
                return client;
            }

            throw new ArgumentException(string.Format(
                CultureInfo.InvariantCulture,
                StitchingResources.SchemaName_NotFound,
                schemaName));
        }

        public ISchema GetRemoteSchema(NameString schemaName)
        {
            schemaName.EnsureNotEmpty(nameof(schemaName));

            if (_clients.TryGetValue(schemaName, out RemoteRequestExecutor client))
            {
                return client.Executor.Schema;
            }

            throw new ArgumentException(string.Format(
                CultureInfo.InvariantCulture,
                StitchingResources.SchemaName_NotFound,
                schemaName));
        }

        public IDisposable Subscribe(IObserver<IRemoteRequestExecutor> observer)
        {
            lock (_sync)
            {
                _observers.Add(observer);
            }

            foreach (RemoteRequestExecutor client in _clients.Values)
            {
                observer.OnNext(client);
            }

            return new Subscription(() =>
            {
                lock (_sync)
                {
                    _observers.Remove(observer);
                }
            });
        }

        private sealed class Subscription
            : IDisposable
        {
            private readonly Action _removeSubscriber;
            private bool _disposed;

            public Subscription(Action removeSubscriber)
            {
                _removeSubscriber = removeSubscriber
                    ?? throw new ArgumentNullException(
                        nameof(removeSubscriber));
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _removeSubscriber.Invoke();
                    _disposed = true;
                }
            }
        }
    }
}
