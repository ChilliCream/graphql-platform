namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class DataStopMessage
        : OperationMessage
    {
        public DataStopMessage(string id)
            : base(MessageTypes.Subscription.Stop, id)
        {
        }
    }
}
