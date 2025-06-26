using System.Net.WebSockets;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Transport.Serialization;
using static System.Net.WebSockets.WebSocketMessageType;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal static class MessageHelper
{
    public static async ValueTask SendConnectionInitMessage(
        this WebSocket socket,
        JsonElement payload,
        CancellationToken ct)
    {
        if (payload.ValueKind is not JsonValueKind.Object and not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            throw new ArgumentException(
                "The payload must be an object, null, or undefined.",
                nameof(payload));
        }

        using var arrayWriter = new PooledArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(Utf8MessageProperties.TypeProp, Utf8Messages.ConnectionInitialize);

        if (payload.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            jsonWriter.WritePropertyName(Utf8MessageProperties.PayloadProp);
            payload.WriteTo(jsonWriter);
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
        using var arrayWriter = new PooledArrayWriter();
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
        using var arrayWriter = new PooledArrayWriter();
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
        using var arrayWriter = new PooledArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(Utf8MessageProperties.TypeProp, Utf8Messages.Pong);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

        await socket.SendAsync(arrayWriter.GetWrittenMemory(), Text, true, ct).ConfigureAwait(false);
    }
}
