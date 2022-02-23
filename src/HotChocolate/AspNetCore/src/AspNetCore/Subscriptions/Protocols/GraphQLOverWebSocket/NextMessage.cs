namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed  class NextMessage : OperationMessage<IExecutionResult>
{
    public NextMessage(IExecutionResult payload, string id)
        : base(Messages.Next, payload)
    {
        Id = id;
    }

    public string Id { get; }
}
