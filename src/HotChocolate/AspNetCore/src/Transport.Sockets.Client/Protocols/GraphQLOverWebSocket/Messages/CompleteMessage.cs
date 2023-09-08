using System.Text.Json;
using HotChocolate.Transport.Serialization;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

internal sealed class CompleteMessage : IDataMessage
{
    private CompleteMessage(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public string Type => Messages.Complete;

    public static CompleteMessage From(JsonDocument document)
    {
        var root = document.RootElement;
        var id = root.GetProperty(Utf8MessageProperties.IdProp).GetString()!;
        return new CompleteMessage(id);
    }
}
