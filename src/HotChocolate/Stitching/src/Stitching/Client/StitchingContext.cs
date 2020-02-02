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
        private readonly List<IObserver<IRemoteQueryClient>> _observers =
            new List<IObserver<IRemoteQueryClient>>();
        private readonly IDictionary<NameString, RemoteQueryClient> _clients;

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
                t => new RemoteQueryClient(services, t.Executor));
        }

        public IRemoteQueryClient GetRemoteQueryClient(NameString schemaName)
        {
            schemaName.EnsureNotEmpty(nameof(schemaName));

            if (_clients.TryGetValue(schemaName, out RemoteQueryClient client))
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

            if (_clients.TryGetValue(schemaName, out RemoteQueryClient client))
            {
                return client.Executor.Schema;
            }

            throw new ArgumentException(string.Format(
                CultureInfo.InvariantCulture,
                StitchingResources.SchemaName_NotFound,
                schemaName));
        }

        public IDisposable Subscribe(IObserver<IRemoteQueryClient> observer)
        {
            lock (_sync)
            {
                _observers.Add(observer);
            }

            foreach (RemoteQueryClient client in _clients.Values)
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
