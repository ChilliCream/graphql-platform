using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.Messages;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

public sealed class InitializeConnectionMessage
    : OperationMessage<IReadOnlyDictionary<string, object?>?>
{
    public InitializeConnectionMessage(IReadOnlyDictionary<string, object?>? payload = null)
        : base(ConnectionInitialize, payload)
    {
    }
}
