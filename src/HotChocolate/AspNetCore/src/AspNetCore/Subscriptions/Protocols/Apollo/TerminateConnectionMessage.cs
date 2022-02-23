using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.Messages;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class TerminateConnectionMessage : OperationMessage
{
    public TerminateConnectionMessage()
        : base(ConnectionTerminate)
    {
    }

    public static TerminateConnectionMessage Default { get; } = new();
}
