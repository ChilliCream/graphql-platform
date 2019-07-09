using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public class InitializeConnectionMessage
        : OperationMessage<IReadOnlyDictionary<string, object>>
    {
        public InitializeConnectionMessage(
            IReadOnlyDictionary<string, object> payload)
            : base(MessageTypes.Connection.Initialize, payload)
        {
        }
    }
}
