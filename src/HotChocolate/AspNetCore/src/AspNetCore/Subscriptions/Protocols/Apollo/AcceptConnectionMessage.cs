using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.Messages;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class AcceptConnectionMessage : OperationMessage
{
    public AcceptConnectionMessage() : base(ConnectionAccept)
    {
    }

    public static AcceptConnectionMessage Default { get; } = new();
}
