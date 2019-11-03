using HotChocolate.Language;

namespace StrawberryShake.Http.Subscriptions
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
