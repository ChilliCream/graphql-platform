namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed class ErrorMessage : OperationMessage<IError[]>
{
    public ErrorMessage(IError[] payload, string id)
        : base(Messages.Error, payload)
    {
        Id = id;
    }

    public string Id { get; }
}
