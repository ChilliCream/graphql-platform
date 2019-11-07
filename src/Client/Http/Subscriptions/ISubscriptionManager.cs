using System;
using System.Collections.Generic;

namespace StrawberryShake.Http.Subscriptions
{
    public interface ISubscriptionManager
        : IEnumerable<ISubscription>
        , IDisposable
    {
        bool TryGetSubscription(string subscriptionId, out ISubscription subscription);

        void Register(ISubscription subscription);

        void Unregister(string subscriptionId);
    }
}
