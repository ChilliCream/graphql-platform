using System.Text.Json;
using HotChocolate.Transport.Serialization;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

internal sealed class ErrorMessage : IDataMessage
{
    private ErrorMessage(string id, OperationResult payload)
    {
        Id = id;
        Payload = payload;
    }

    public string Id { get; }

    public string Type => Messages.Error;

    public OperationResult Payload { get; }

    public static ErrorMessage From(JsonDocument document)
    {
        var root = document.RootElement;
        var id = root.GetProperty(Utf8MessageProperties.IdProp).GetString()!;

        var payload = root.GetProperty(Utf8MessageProperties.PayloadProp);
        var result = new OperationResult(document, errors: payload);

        return new ErrorMessage(id, result);
    }
}
