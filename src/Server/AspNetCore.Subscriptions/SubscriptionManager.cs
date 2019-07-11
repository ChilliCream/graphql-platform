using System.Linq;
using System;
using System.Collections.Concurrent;
using HotChocolate.Execution;
using HotChocolate.Server;
using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SubscriptionManager
        : ISubscriptionManager
    {
        private readonly ConcurrentDictionary<string, ISubscription> _subs =
            new ConcurrentDictionary<string, ISubscription>();
        private readonly ISocketConnection _connection;
        private bool _disposed;

        public SubscriptionManager(ISocketConnection connection)
        {
            _connection = connection
                ?? throw new ArgumentNullException(nameof(connection));
        }

        public void Register(
            string subscriptionId,
            IResponseStream responseStream)
        {
            Register(new Subscription(
                _connection,
                responseStream,
                subscriptionId));
        }

        public void Register(ISubscription subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(SubscriptionManager));
            }

            if (_subs.TryAdd(subscription.Id, subscription))
            {
                subscription.Completed += (sender, eventArgs) =>
                {
                    Unregister(subscription.Id);
                };
            }
        }

        public void Unregister(ISubscription subscription)
        {
            if (subscription is null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(SubscriptionManager));
            }

            Unregister(subscription.Id);
        }

        public void Unregister(string subscriptionId)
        {
            if (subscriptionId == null)
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(SubscriptionManager));
            }

            if (_subs.TryRemove(subscriptionId,
                out ISubscription subscription))
            {
                subscription.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_subs.Count > 0)
                    {
                        ISubscription[] subs = _subs.Values.ToArray();

                        for (int i = 0; i < subs.Length; i++)
                        {
                            subs[i].Dispose();
                            subs[i] = null;
                        }

                        _subs.Clear();
                    }
                }
                _disposed = true;
            }
        }

        public IEnumerator<ISubscription> GetEnumerator()
        {
            return _subs.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
