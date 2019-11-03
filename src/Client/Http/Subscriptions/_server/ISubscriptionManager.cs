using System;
using System.Collections.Generic;

namespace StrawberryShake.Http.Subscriptions
{
    public interface ISubscriptionManager
        : IEnumerable<ISubscription>
        , IDisposable
    {
        void Register(ISubscription subscription);

        void Unregister(string subscriptionId);
    }
}
