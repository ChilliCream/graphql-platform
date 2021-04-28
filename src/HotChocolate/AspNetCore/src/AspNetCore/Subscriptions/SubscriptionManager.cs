using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly ConcurrentDictionary<string, ISubscriptionSession> _subs =
            new ConcurrentDictionary<string, ISubscriptionSession>();
        private readonly ISocketConnection _connection;
        private bool _disposed;

        public SubscriptionManager(ISocketConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public void Register(ISubscriptionSession subscriptionSession)
        {
            if (subscriptionSession == null)
            {
                throw new ArgumentNullException(nameof(subscriptionSession));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscriptionManager));
            }

            if (_subs.TryAdd(subscriptionSession.Id, subscriptionSession))
            {
                subscriptionSession.Completed += (sender, eventArgs) =>
                {
                    Unregister(subscriptionSession.Id);
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
                throw new ObjectDisposedException(nameof(SubscriptionManager));
            }

            if (_subs.TryRemove(subscriptionId, out ISubscriptionSession? subscription))
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
                    ISubscriptionSession?[] subs = _subs.Values.ToArray();
                    _subs.Clear();

                    for (int i = 0; i < subs.Length; i++)
                    {
                        subs[i]?.Dispose();
                        subs[i] = null;
                    }
                }
                _disposed = true;
            }
        }

        public IEnumerator<ISubscriptionSession> GetEnumerator()
        {
            return _subs.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
