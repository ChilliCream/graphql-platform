using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

/// <summary>
/// A base class for operation messages.
/// </summary>
public abstract class OperationMessage
{
    protected OperationMessage(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            throw new ArgumentException(OperationMessage_TypeCannotBeNullOrEmpty, nameof(type));
        }

        Type = type;
    }

    /// <summary>
    /// Gets the operation message type.
    /// </summary>
    public string Type { get; }
}
