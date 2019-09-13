using System;
using System.Collections.Generic;

namespace HotChocolate.Server
{
    public interface ISubscriptionManager
        : IEnumerable<ISubscription>
        , IDisposable
    {
        void Register(ISubscription subscription);

        void Unregister(string subscriptionId);
    }
}
