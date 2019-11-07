using System;
using System.Collections.Generic;

namespace StrawberryShake.Http.Subscriptions
{
    public interface ISubscriptionManager
        : IEnumerable<ISubscription>
        , IDisposable
    {
        bool TryGetSubscription(string subscriptionId, out ISubscription subscription);

        void RegisterAsync(ISubscription subscription, ISocketConnection connection);

        void UnregisterAsync(string subscriptionId);
    }
}
