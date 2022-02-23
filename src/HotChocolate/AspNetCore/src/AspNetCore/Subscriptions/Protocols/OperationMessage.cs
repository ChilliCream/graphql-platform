using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

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

    public string Type { get; }
}
