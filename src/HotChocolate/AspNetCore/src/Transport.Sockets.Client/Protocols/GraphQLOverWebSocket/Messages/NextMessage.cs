using System;
using System.Text.Json;
using HotChocolate.Transport.Serialization;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

internal sealed class NextMessage : IDataMessage
{
    private NextMessage(string id, OperationResult payload)
    {
        Id = id;
        Payload = payload;
    }

    public string Id { get; }

    public string Type => Messages.Next;

    public OperationResult Payload { get; }

    public static NextMessage From(JsonDocument document)
    {
        var root = document.RootElement;
        var id = root.GetProperty(Utf8MessageProperties.IdProp).GetString()!;

        var payload = root.GetProperty(Utf8MessageProperties.PayloadProp);
        var result = new OperationResult(
            document,
            TryGetProperty(payload, Utf8MessageProperties.DataProp),
            TryGetProperty(payload, Utf8MessageProperties.ErrorsProp),
            TryGetProperty(payload, Utf8MessageProperties.ExtensionsProp));

        return new NextMessage(id, result);
    }

    private static JsonElement TryGetProperty(JsonElement element, ReadOnlySpan<byte> name)
        => element.TryGetProperty(name, out var property)
            ? property
            : default;
}
