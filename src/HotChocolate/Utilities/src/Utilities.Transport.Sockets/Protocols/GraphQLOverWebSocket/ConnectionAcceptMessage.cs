namespace HotChocolate.Utilities.Transport.Sockets.Protocols.GraphQLOverWebSocket;

internal sealed class ConnectionAcceptMessage : OperationMessage
{
    public override string Type => Messages.ConnectionAccept;

    public static ConnectionAcceptMessage Default { get; } = new();
}
