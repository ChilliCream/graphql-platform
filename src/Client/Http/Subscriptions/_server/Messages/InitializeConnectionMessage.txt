using System.Collections.Generic;

namespace StrawberryShake.Http.Subscriptions
{
    public sealed class InitializeConnectionMessage
        : OperationMessage<IReadOnlyDictionary<string, object>>
    {
        public InitializeConnectionMessage(
            IReadOnlyDictionary<string, object> payload)
            : base(MessageTypes.Connection.Initialize, payload)
        {
        }
    }
}
