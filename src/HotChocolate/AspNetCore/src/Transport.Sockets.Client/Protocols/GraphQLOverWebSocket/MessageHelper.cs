using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Sockets.Client.Helpers;
using static System.Net.WebSockets.WebSocketMessageType;
using static HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Utf8MessageProperties;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal static class MessageHelper
{
    public static async ValueTask SendConnectionInitMessage<T>(
        this WebSocket socket,
        T payload,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(TypeProp, Utf8Messages.ConnectionInitialize);

        if (payload is not null)
        {
            jsonWriter.WritePropertyName(PayloadProp);
            JsonSerializer.Serialize(jsonWriter, payload, JsonDefaults.SerializerOptions);
        }

        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

#if NET5_0_OR_GREATER
        await socket.SendAsync(arrayWriter.Body, Text, true, ct).ConfigureAwait(false);
#else
        await socket.SendAsync(arrayWriter.ToArraySegment(), Text, true, ct).ConfigureAwait(false);
#endif
    }

    public static async ValueTask SendSubscribeMessageAsync(
        this WebSocket socket,
        string operationSessionId,
        OperationRequest request,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(IdProp, operationSessionId);
        jsonWriter.WriteString(TypeProp, Utf8Messages.Subscribe);
        jsonWriter.WritePropertyName(PayloadProp);
        JsonSerializer.Serialize(jsonWriter, request, JsonDefaults.SerializerOptions);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

#if NET5_0_OR_GREATER
        await socket.SendAsync(arrayWriter.Body, Text, true, ct).ConfigureAwait(false);
#else
        await socket.SendAsync(arrayWriter.ToArraySegment(), Text, true, ct).ConfigureAwait(false);
#endif
    }

    public static async ValueTask SendCompleteMessageAsync(
        this WebSocket socket,
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
        await socket.SendAsync(arrayWriter.Body, Text, true, ct).ConfigureAwait(false);
#else
        await socket.SendAsync(arrayWriter.ToArraySegment(), Text, true, ct).ConfigureAwait(false);
#endif
    }

    public static async ValueTask SendPongMessageAsync(
        this WebSocket socket,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(TypeProp, Utf8Messages.Pong);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

#if NET5_0_OR_GREATER
        await socket.SendAsync(arrayWriter.Body, Text, true, ct).ConfigureAwait(false);
#else
        await socket.SendAsync(arrayWriter.ToArraySegment(), Text, true, ct).ConfigureAwait(false);
#endif
    }
}
