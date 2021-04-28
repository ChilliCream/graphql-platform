using System;
using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public interface ISubscriptionManager
        : IEnumerable<ISubscriptionSession>
        , IDisposable
    {
        void Register(ISubscriptionSession subscriptionSession);

        void Unregister(string subscriptionId);
    }
}
