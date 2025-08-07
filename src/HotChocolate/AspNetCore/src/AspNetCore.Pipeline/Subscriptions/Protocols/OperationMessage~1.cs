namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

/// <summary>
/// A base class for operation messages that have a payload.
/// </summary>
public abstract class OperationMessage<T> : OperationMessage
{
    protected OperationMessage(string type, T payload)
        : base(type)
    {
        Payload = payload;
    }

    /// <summary>
    /// Gets the operation message payload.
    /// </summary>
    public T Payload { get; }
}
