namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

public abstract class OperationMessage<T> : OperationMessage
{
    protected OperationMessage(string type, T payload)
        : base(type)
    {
        Payload = payload;
    }

    public T Payload { get; }
}
