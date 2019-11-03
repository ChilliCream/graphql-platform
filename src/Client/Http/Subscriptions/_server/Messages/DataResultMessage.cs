namespace StrawberryShake.Http.Subscriptions
{
    public sealed class DataResultMessage<T>
        : OperationMessage<IOperationResult<T>>
        where T : class
    {
        public DataResultMessage(string id, IOperationResult<T> payload)
            : base(
                MessageTypes.Subscription.Data,
                id,
                payload)
        {
        }
    }
}
