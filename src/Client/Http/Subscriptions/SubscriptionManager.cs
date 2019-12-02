using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions.Messages;
using StrawberryShake.Transport;

namespace StrawberryShake.Http.Subscriptions
{
    public sealed class SubscriptionManager
        : ISubscriptionManager
    {
        private readonly ConcurrentDictionary<string, Sub> _subs =
            new ConcurrentDictionary<string, Sub>();
        private readonly IOperationFormatter _operationFormatter;
        private bool _disposed;

        public SubscriptionManager(IOperationFormatter operationFormatter)
        {
            _operationFormatter = operationFormatter;
        }

        public bool TryGetSubscription(string subscriptionId, out ISubscription? subscription)
        {
            if (_subs.TryGetValue(subscriptionId, out Sub? sub))
            {
                subscription = sub.Subscription;
                return true;
            }

            subscription = null;
            return false;
        }

        public Task RegisterAsync(ISubscription subscription, ISocketConnection connection)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            return RegisterInternalAsync(subscription, connection);
        }

        private async Task RegisterInternalAsync(
            ISubscription subscription,
            ISocketConnection connection)
        {
            if (_subs.TryAdd(subscription.Id, new Sub(subscription, connection)))
            {
                connection.Disposed += (sender, args) => RemoveSubscription(subscription.Id);

                if (connection.IsClosed)
                {
                    _subs.TryRemove(subscription.Id, out _);
                    return;
                }

                subscription.OnRegister(() => UnregisterAsync(subscription.Id));

                var writer = new SocketMessageWriter();

                writer.WriteStartObject();
                writer.WriteType(MessageTypes.Subscription.Start);
                writer.WriteId(subscription.Id);
                writer.WriteStartPayload();
                _operationFormatter.Serialize(subscription.Operation, writer);
                writer.WriteEndObject();

                await connection.SendAsync(writer.Body).ConfigureAwait(false);
            }
        }

        public async Task UnregisterAsync(string subscriptionId)
        {
            if (_subs.TryRemove(subscriptionId, out Sub? sub))
            {
                var writer = new SocketMessageWriter();

                writer.WriteStartObject();
                writer.WriteType(MessageTypes.Subscription.Stop);
                writer.WriteId(subscriptionId);
                writer.WriteEndObject();

                await sub.Connection.SendAsync(writer.Body);
            }
        }

        private void RemoveSubscription(string subscriptionId)
        {
            if (_subs.TryRemove(subscriptionId, out Sub? sub))
            {
                BeginDisposeSubscription(sub.Subscription);
            }
        }

        private void BeginDisposeSubscription(ISubscription subscription)
        {
            Task.Run(async () =>
            {
                if (subscription is IAsyncDisposable d)
                {
                    await d.DisposeAsync().ConfigureAwait((false));
                }
            });
        }

        public void Dispose()
        {
            if (!_disposed && _subs.Count > 0)
            {
                Sub?[] subs = _subs.Values.ToArray();

                for (var i = 0; i < subs.Length; i++)
                {
                    BeginDisposeSubscription(subs[i]!.Subscription);
                    subs[i] = null;
                }

                _subs.Clear();
                _disposed = true;
            }
        }

        public IEnumerator<ISubscription> GetEnumerator()
        {
            return _subs.Values.Select(t => t.Subscription).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Sub
        {
            public Sub(ISubscription subscription, ISocketConnection connection)
            {
                Subscription = subscription;
                Connection = connection;
            }

            public ISubscription Subscription { get; }

            public ISocketConnection Connection { get; }
        }
    }
}
