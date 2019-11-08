using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Subscriptions
{
    public interface ISubscriptionManager
        : IEnumerable<ISubscription>
        , IDisposable
    {
        bool TryGetSubscription(string subscriptionId, out ISubscription subscription);

        Task RegisterAsync(ISubscription subscription, ISocketConnection connection);

        Task UnregisterAsync(string subscriptionId);
    }
}
