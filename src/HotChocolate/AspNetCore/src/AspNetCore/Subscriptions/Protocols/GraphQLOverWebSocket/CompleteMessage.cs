namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed class CompleteMessage : OperationMessage
{
    public CompleteMessage(string id)
        : base(Messages.Complete)
    {
        Id = id;
    }

    public string Id { get; }
}
