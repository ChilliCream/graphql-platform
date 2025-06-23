namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

/// <summary>
/// A base class for operation messages.
/// </summary>
public abstract class OperationMessage
{
    protected OperationMessage(string type)
    {
        ArgumentException.ThrowIfNullOrEmpty(type);

        Type = type;
    }

    /// <summary>
    /// Gets the operation message type.
    /// </summary>
    public string Type { get; }
}
