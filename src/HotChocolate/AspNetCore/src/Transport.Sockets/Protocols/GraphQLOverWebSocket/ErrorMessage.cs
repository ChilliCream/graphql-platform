using System.Collections.Generic;
using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

public sealed class ErrorMessage : IMessage
{

    public ErrorMessage(string id, IError payload)
    {
        Id = id;
        Payload = new[] { payload };
    }

    public ErrorMessage(string id, IReadOnlyList<IError> payload)
    {
        Id = id;
        Payload = payload;
    }

    public string Type => MessageTypes.Error;

    public string Id { get; }

    public IReadOnlyList<IError> Payload { get; }
}
