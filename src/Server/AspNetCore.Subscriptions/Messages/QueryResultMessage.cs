using System.Collections.Generic;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class QueryResultMessage
        : OperationMessage<IReadOnlyDictionary<string, object>>
    {
        public QueryResultMessage(string id, IReadOnlyQueryResult payload)
            : base(
                MessageTypes.Subscription.Data,
                id,
                payload.ToDictionary())
        {
        }
    }

    public sealed class QueryResultMessage
        : OperationMessage<IReadOnlyDictionary<string, object>>
    {
        public QueryResultMessage(string id, IReadOnlyQueryResult payload)
            : base(
                MessageTypes.Subscription.Data,
                id,
                payload.ToDictionary())
        {
        }
    }
}
