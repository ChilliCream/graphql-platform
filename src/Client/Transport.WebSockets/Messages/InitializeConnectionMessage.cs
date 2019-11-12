using System.Collections.Generic;

namespace StrawberryShake.Transport.WebSockets.Messages
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
