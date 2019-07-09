using HotChocolate.Execution;

namespace HotChocolate.Server
{
    public interface ISubscriptionManager
    {
        void Register(string subscriptionId, IResponseStream responseStream);

        void Register(ISubscription subscription);

        void Unregister(ISubscription subscription);

        void Unregister(string subscriptionId);
    }
}
