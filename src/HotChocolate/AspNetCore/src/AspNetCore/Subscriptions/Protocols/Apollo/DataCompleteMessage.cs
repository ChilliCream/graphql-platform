using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.Messages;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class DataCompleteMessage : OperationMessage
{
    public DataCompleteMessage(string id)
        : base(Complete)
    {
        Id = id;
    }

    public string Id { get; }
}
