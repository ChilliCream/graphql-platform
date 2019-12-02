using HotChocolate.Language;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions.Messages
{
    public sealed class DataStartMessage
        : OperationMessage<GraphQLRequest>
    {
        public DataStartMessage(string id, GraphQLRequest request)
            : base(MessageTypes.Subscription.Start, id, request)
        {
        }
    }
}
