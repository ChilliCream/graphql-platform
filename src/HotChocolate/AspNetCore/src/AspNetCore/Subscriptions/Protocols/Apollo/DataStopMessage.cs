using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.Messages;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class DataStopMessage : OperationMessage
{
    public DataStopMessage(string id)
        : base(Stop)
    {
        Id = id;
    }

    public string Id { get; }
}
