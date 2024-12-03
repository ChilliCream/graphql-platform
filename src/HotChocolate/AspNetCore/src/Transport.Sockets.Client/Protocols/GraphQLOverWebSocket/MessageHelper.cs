using System.Net.WebSockets;
using System.Text.Json;
using HotChocolate.Transport.Serialization;
using HotChocolate.Utilities;
using static System.Net.WebSockets.WebSocketMessageType;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal static class MessageHelper
{
    public static async ValueTask SendConnectionInitMessage<T>(
        this WebSocket socket,
        T payload,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(Utf8MessageProperties.TypeProp, Utf8Messages.ConnectionInitialize);

        if (payload is not null)
        {
            jsonWriter.WritePropertyName(Utf8MessageProperties.PayloadProp);
            JsonSerializer.Serialize(jsonWriter, payload, JsonOptionDefaults.SerializerOptions);
        }

        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

        await socket.SendAsync(arrayWriter.GetWrittenMemory(), Text, true, ct).ConfigureAwait(false);
    }

    public static async ValueTask SendSubscribeMessageAsync(
        this WebSocket socket,
        string operationSessionId,
        OperationRequest request,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);

        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(Utf8MessageProperties.IdProp, operationSessionId);
        jsonWriter.WriteString(Utf8MessageProperties.TypeProp, Utf8Messages.Subscribe);
        jsonWriter.WritePropertyName(Utf8MessageProperties.PayloadProp);

        request.WriteTo(jsonWriter);

        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

        await socket.SendAsync(arrayWriter.GetWrittenMemory(), Text, true, ct).ConfigureAwait(false);
    }

    public static async ValueTask SendCompleteMessageAsync(
        this WebSocket socket,
        string operationSessionId,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(Utf8MessageProperties.IdProp, operationSessionId);
        jsonWriter.WriteString(Utf8MessageProperties.TypeProp, Utf8Messages.Complete);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

        await socket.SendAsync(arrayWriter.GetWrittenMemory(), Text, true, ct).ConfigureAwait(false);
    }

    public static async ValueTask SendPongMessageAsync(
        this WebSocket socket,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(Utf8MessageProperties.TypeProp, Utf8Messages.Pong);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

        await socket.SendAsync(arrayWriter.GetWrittenMemory(), Text, true, ct).ConfigureAwait(false);
    }
}
