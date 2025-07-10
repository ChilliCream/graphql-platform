using HotChocolate.Language;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.Messages;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class DataStartMessage : OperationMessage<GraphQLRequest>
{
    public DataStartMessage(string id, GraphQLRequest request)
        : base(Start, request)
    {
        Id = id;
    }

    public string Id { get; }
}
