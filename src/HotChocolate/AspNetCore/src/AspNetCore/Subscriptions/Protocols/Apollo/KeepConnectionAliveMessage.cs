using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.Messages;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class KeepConnectionAliveMessage : OperationMessage
{
    private KeepConnectionAliveMessage()
        : base(KeepAlive)
    {
    }

    public static KeepConnectionAliveMessage Default { get; } = new();
}
