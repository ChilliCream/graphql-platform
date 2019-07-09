using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public class InitializeConnectionMessage
        : OperationMessage<IDictionary<string, object>>
    {
        public InitializeConnectionMessage(IDictionary<string, object> payload)
            : base(MessageTypes.Connection.Initialize, payload)
        {
        }
    }
}
