using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Subscriptions
{
    public interface ISubscriptionManager
    {
        object Subscribe(QueryRequest queryRequest, string eventName, object args);
        void Unsubscribe(string subscriptionId);
        object Restore(string subscriptionId);
    }
}

