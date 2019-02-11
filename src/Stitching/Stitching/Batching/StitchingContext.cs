﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public class StitchingContext
        : IStitchingContext
    {
        private readonly object _sync = new object();
        private readonly List<IObserver<IRemoteQueryClient>> _observers =
            new List<IObserver<IRemoteQueryClient>>();
        private readonly IDictionary<string, RemoteQueryClient> _clients;

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

        public IRemoteQueryClient GetRemoteQueryClient(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name cannot be null or empty.",
                    nameof(schemaName));
            }

            if (_clients.TryGetValue(schemaName, out RemoteQueryClient client))
            {
                return client;
            }

            throw new ArgumentException(
                $"There is now shema with the given name `{schemaName}`.");
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
