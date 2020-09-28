using System.Collections.Generic;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class DataResultMessage
        : OperationMessage<IReadOnlyDictionary<string, object?>>
    {
        public DataResultMessage(string id, IQueryResult payload)
            : base(
                MessageTypes.Subscription.Data,
                id,
                payload.ToDictionary())
        {
        }
    }
}
