using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions.Messages
{
    public sealed class DataResultMessage
        : OperationMessage<IOperationResultBuilder>
    {
        public DataResultMessage(string id, IOperationResultBuilder payload)
            : base(
                MessageTypes.Subscription.Data,
                id,
                payload)
        {
        }
    }
}
