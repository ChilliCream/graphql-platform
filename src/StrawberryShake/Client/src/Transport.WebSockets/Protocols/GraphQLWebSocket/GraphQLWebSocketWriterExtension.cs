using System.Text.Json;
using StrawberryShake.Json;
using static StrawberryShake.Transport.WebSockets.Protocols.GraphQLWebSocketMessageTypeSpans;

namespace StrawberryShake.Transport.WebSockets.Protocols;

/// <summary>
/// Common extension of the <see cref="SocketMessageWriter"/> for
/// <see cref="GraphQLWebSocketProtocol"/>
/// </summary>
internal static class GraphQLWebSocketWriterExtension
{
    /// <summary>
    /// Writes a <see cref="GraphQLWebSocketMessageType.Start"/> message to the writer
    /// </summary>
    /// <param name="writer">The writer</param>
    /// <param name="operationId">The operation id of the operation</param>
    /// <param name="request">The operation request containing the payload</param>
    public static void WriteStartOperationMessage(
        this SocketMessageWriter writer,
        string operationId,
        OperationRequest request)
    {
        if (operationId == null)
        {
            throw new ArgumentNullException(nameof(operationId));
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        writer.WriteStartObject();
        writer.WriteType(GraphQLWebSocketMessageType.Start);
        writer.WriteId(operationId);
        writer.WriteStartPayload();
        writer.Serialize(request);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes a <see cref="GraphQLWebSocketMessageType.Stop"/> message to the writer
    /// </summary>
    /// <param name="writer">The writer</param>
    /// <param name="operationId">The operation id of the operation</param>
    public static void WriteStopOperationMessage(
        this SocketMessageWriter writer,
        string operationId)
    {
        if (operationId == null)
        {
            throw new ArgumentNullException(nameof(operationId));
        }

        writer.WriteStartObject();
        writer.WriteType(GraphQLWebSocketMessageType.Stop);
        writer.WriteId(operationId);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes a <see cref="GraphQLWebSocketMessageType.ConnectionInit"/>message to the writer
    /// </summary>
    /// <param name="writer">The writer</param>
    /// <param name="payload">The payload of the init message</param>
    public static void WriteInitializeMessage(
        this SocketMessageWriter writer,
        object? payload)
    {
        writer.WriteStartObject();
        writer.WriteType(GraphQLWebSocketMessageType.ConnectionInit);

        if (payload is not null)
        {
            writer.WriteStartPayload();
            JsonSerializer.Serialize(writer.Writer, payload);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes a <see cref="GraphQLWebSocketMessageType.ConnectionTerminate"/> message to the
    /// writer
    /// </summary>
    /// <param name="writer">The writer</param>
    public static void WriteTerminateMessage(this SocketMessageWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteType(GraphQLWebSocketMessageType.ConnectionTerminate);
        writer.WriteEndObject();
    }

    private static void WriteId(
        this SocketMessageWriter writer,
        string id)
    {
        writer.Writer.WritePropertyName("id");
        writer.Writer.WriteStringValue(id);
    }

    private static void WriteType(
        this SocketMessageWriter writer,
        GraphQLWebSocketMessageType type)
    {
        writer.Writer.WritePropertyName("type");
        var typeToWriter = type switch
        {
            GraphQLWebSocketMessageType.ConnectionInit => ConnectionInitialize,
            GraphQLWebSocketMessageType.ConnectionAccept => ConnectionAccept,
            GraphQLWebSocketMessageType.ConnectionError => ConnectionError,
            GraphQLWebSocketMessageType.KeepAlive => KeepAlive,
            GraphQLWebSocketMessageType.ConnectionTerminate => ConnectionTerminate,
            GraphQLWebSocketMessageType.Start => Start,
            GraphQLWebSocketMessageType.Data => Data,
            GraphQLWebSocketMessageType.Error => Error,
            GraphQLWebSocketMessageType.Complete => Complete,
            GraphQLWebSocketMessageType.Stop => Stop,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        writer.Writer.WriteStringValue(typeToWriter);
    }

    private static void WriteStartPayload(this SocketMessageWriter writer)
    {
        writer.Writer.WritePropertyName("payload");
    }

    private static void Serialize(
        this SocketMessageWriter writer,
        OperationRequest request)
    {
        JsonOperationRequestSerializer.Default.Serialize(request, writer.Writer);
    }
}
