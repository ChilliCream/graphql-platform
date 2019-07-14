namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class StartOperationMessage
        : OperationMessage
    {
        public SubscriptionQuery Payload { get; set; }
    }
}
