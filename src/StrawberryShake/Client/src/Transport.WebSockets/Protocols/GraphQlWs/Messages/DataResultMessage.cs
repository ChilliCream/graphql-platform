using System.Text.Json;
using StrawberryShake.Transport;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions.Messages
{
    public sealed class DataResultMessage
        : OperationMessage<JsonDocument>
    {
        public DataResultMessage(string id, JsonDocument payload)
            : base(
                MessageTypes.Subscription.Data,
                id,
                payload)
        {
        }
    }
}
