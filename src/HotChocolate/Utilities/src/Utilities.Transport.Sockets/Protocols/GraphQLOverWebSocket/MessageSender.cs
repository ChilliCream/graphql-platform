using System.Net.WebSockets;
using System.Text.Json;
using HotChocolate.Utilities.Transport.Sockets.Helpers;
using static System.Net.WebSockets.WebSocketMessageType;
using static HotChocolate.Utilities.Transport.Sockets.Protocols.GraphQLOverWebSocket.Utf8MessageProperties;

namespace HotChocolate.Utilities.Transport.Sockets.Protocols.GraphQLOverWebSocket;

internal sealed class MessageSender
{
    private WebSocket _socket;

    public MessageSender(WebSocket socket)
    {
        _socket = socket;
    }

    public async ValueTask SendConnectionInitMessage<T>(T payload, CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(TypeProp, Utf8Messages.ConnectionInitialize);
        jsonWriter.WritePropertyName(PayloadProp);
        JsonSerializer.Serialize(jsonWriter, payload, JsonDefaults.SerializerOptions);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

#if NET5_0_OR_GREATER
        await _socket.SendAsync(arrayWriter.Body, Text, true, ct).ConfigureAwait(false);
#else
        await _socket.SendAsync(arrayWriter.ToArraySegment(), Text, true, ct).ConfigureAwait(false);
#endif
    }

    public async ValueTask SendSubscribeMessageAsync(
        string operationSessionId,
        OperationRequest request,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(IdProp, operationSessionId);
        jsonWriter.WriteString(TypeProp, Utf8Messages.ConnectionInitialize);
        jsonWriter.WritePropertyName(PayloadProp);
        JsonSerializer.Serialize(jsonWriter, request, JsonDefaults.SerializerOptions);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

#if NET5_0_OR_GREATER
        await _socket.SendAsync(arrayWriter.Body, Text, true, ct).ConfigureAwait(false);
#else
        await _socket.SendAsync(arrayWriter.ToArraySegment(), Text, true, ct).ConfigureAwait(false);
#endif
    }

    public async ValueTask SendCompleteMessageAsync(
        string operationSessionId,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(IdProp, operationSessionId);
        jsonWriter.WriteString(TypeProp, Utf8Messages.Complete);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

#if NET5_0_OR_GREATER
        await _socket.SendAsync(arrayWriter.Body, Text, true, ct).ConfigureAwait(false);
#else
        await _socket.SendAsync(arrayWriter.ToArraySegment(), Text, true, ct).ConfigureAwait(false);
#endif
    }
}
