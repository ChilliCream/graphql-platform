namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal sealed class ConnectionAcceptMessage : IOperationMessage
{
    public string Type => Messages.Messages.ConnectionAccept;

    public static ConnectionAcceptMessage Default { get; } = new();
}
