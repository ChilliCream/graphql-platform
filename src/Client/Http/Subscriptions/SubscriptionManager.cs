using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace StrawberryShake.Http.Subscriptions
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
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public bool TryGetSubscription(string subscriptionId, out ISubscription? subscription) =>
            _subs.TryGetValue(subscriptionId, out subscription);


        public void RegisterAsync(ISubscription subscription, ISocketConnection connection)
        {
            connection.Disposed += (sender, args) => UnregisterInternal(subscription.Id);
            subscription.Disposed += (sender, args) => UnregisterInternal(subscription.Id);

            var operation = new GraphQLRequest()
        }

        public void UnregisterAsync(string subscriptionId)
        {
            throw new NotImplementedException();
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
                out ISubscription? subscription))
            {
                subscription.Dispose();
            }
        }

        private void UnregisterInternal(string subscriptionId)
        {
            if (_subs.TryRemove(subscriptionId, out ISubscription? subscription))
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
                if (disposing && _subs.Count > 0)
                {
                    ISubscription?[] subs = _subs.Values.ToArray();

                    for (int i = 0; i < subs.Length; i++)
                    {
                        subs[i]!.Dispose();
                        subs[i] = null;
                    }

                    _subs.Clear();
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
