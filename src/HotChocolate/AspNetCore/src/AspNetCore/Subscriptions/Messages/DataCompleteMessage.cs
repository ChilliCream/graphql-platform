namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class DataCompleteMessage : OperationMessage
    {
        public DataCompleteMessage(string id)
            : base(MessageTypes.Subscription.Complete, id)
        {
        }

        public override string Id => base.Id!;
    }
}
