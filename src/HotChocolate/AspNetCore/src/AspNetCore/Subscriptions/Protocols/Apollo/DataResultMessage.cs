using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.Messages;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class DataResultMessage : OperationMessage<IReadOnlyDictionary<string, object?>>
{
    public DataResultMessage(string id, IQueryResult payload)
        : base(Data, payload.ToDictionary())
    {
        Id = id;
    }

    public string Id { get; }

}
