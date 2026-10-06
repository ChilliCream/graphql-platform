using System.Net.WebSockets;
using System.Text.Json;
using HotChocolate.Buffers;
#if FUSION
using HotChocolate.Fusion.Transport.Serialization;
using HotChocolate.Text.Json;
#else
using HotChocolate.Transport.Serialization;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;
#else
namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;
#endif

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

        await socket.SendAsync(arrayWriter.WrittenMemory, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
    }

#if FUSION
    public static async ValueTask SendSubscribeMessageAsync(
        this WebSocket socket,
        string operationSessionId,
        IOperationRequest request,
        CancellationToken ct)
    {
        using var arrayWriter = new PooledArrayWriter();
        var jsonWriter = new JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);

        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName(Utf8MessageProperties.IdProp);
        jsonWriter.WriteStringValue(operationSessionId);
        jsonWriter.WritePropertyName(Utf8MessageProperties.TypeProp);
        jsonWriter.WriteStringValue(Utf8Messages.Subscribe);
        jsonWriter.WritePropertyName(Utf8MessageProperties.PayloadProp);
        request.WriteTo(jsonWriter);
        jsonWriter.WriteEndObject();

        await socket.SendAsync(arrayWriter.WrittenMemory, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
    }
#else
    public static async ValueTask SendSubscribeMessageAsync(
        this WebSocket socket,
        string operationSessionId,
        IOperationRequest request,
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

        await socket.SendAsync(arrayWriter.WrittenMemory, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
    }
#endif

#if FUSION
    public static async ValueTask SendSubscribeMessageAsync(
        this WebSocket socket,
        string operationSessionId,
        OperationBatchRequest request,
        CancellationToken ct)
    {
        using var arrayWriter = new PooledArrayWriter();
        var jsonWriter = new JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);

        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName(Utf8MessageProperties.IdProp);
        jsonWriter.WriteStringValue(operationSessionId);
        jsonWriter.WritePropertyName(Utf8MessageProperties.TypeProp);
        jsonWriter.WriteStringValue(Utf8Messages.Subscribe);
        jsonWriter.WritePropertyName(Utf8MessageProperties.PayloadProp);
        request.WriteTo(jsonWriter);
        jsonWriter.WriteEndObject();

        await socket.SendAsync(arrayWriter.WrittenMemory, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
    }
#else
    public static async ValueTask SendSubscribeMessageAsync(
        this WebSocket socket,
        string operationSessionId,
        OperationBatchRequest request,
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

        await socket.SendAsync(arrayWriter.WrittenMemory, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
    }
#endif

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

        await socket.SendAsync(arrayWriter.WrittenMemory, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
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

        await socket.SendAsync(arrayWriter.WrittenMemory, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
    }
}
