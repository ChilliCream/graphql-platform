using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

public sealed class CompleteMessage : IMessage
{
    public CompleteMessage(string id)
    {
        Id = id;
    }

    public string Type => MessageTypes.Error;

    public string Id { get; }
}
