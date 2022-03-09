namespace HotChocolate.Utilities.Transport.Sockets.Protocols.GraphQLOverWebSocket;

internal sealed class DataMessage : OperationMessage
{
    public DataMessage(string id, string type, OperationResult payload)
    {
        Id = id;
        Type = type;
        Payload = payload;
    }

    public string Id { get; }

    public override string Type { get; }

    public OperationResult Payload { get; }
}
