using HotChocolate.Execution;
using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

public sealed class NextMessage : IMessage
{
    public NextMessage(string id, IQueryResult payload)
    {
        Id = id;
        Payload = payload;
    }

    public string Type => MessageTypes.Next;

    public string Id { get; }

    public IQueryResult Payload { get; }
}
