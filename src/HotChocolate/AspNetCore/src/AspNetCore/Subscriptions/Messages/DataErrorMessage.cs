namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class DataErrorMessage : OperationMessage
    {
        public DataErrorMessage(string id)
            : base(MessageTypes.Subscription.Error, id)
        {
        }

        public override string Id => base.Id!;
    }
}
