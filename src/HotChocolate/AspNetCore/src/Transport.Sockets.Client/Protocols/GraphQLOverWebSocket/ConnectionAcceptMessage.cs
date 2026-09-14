#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;
#else
namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;
#endif

internal sealed class ConnectionAcceptMessage : IOperationMessage
{
    public string Type => Messages.Messages.ConnectionAccept;

    public static ConnectionAcceptMessage Default { get; } = new();
}
