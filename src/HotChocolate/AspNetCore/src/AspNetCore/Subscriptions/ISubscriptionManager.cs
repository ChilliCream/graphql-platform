using System;
using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public interface ISubscriptionManager
        : IEnumerable<ISubscription>
        , IDisposable
    {
        void Register(ISubscription subscription);

        void Unregister(string subscriptionId);
    }
}
