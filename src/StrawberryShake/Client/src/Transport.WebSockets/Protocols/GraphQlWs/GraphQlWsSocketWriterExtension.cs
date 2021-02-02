using System;
using StrawberryShake.Transport.Http;
using StrawberryShake.Transport.WebSockets;
using static StrawberryShake.Transport.GraphQlWsMessageTypeSpans;

namespace StrawberryShake.Transport
{
    /// <summary>
    /// Common extension of the <see cref="SocketMessageWriter"/> for
    /// <see cref="GraphQlWsProtocol"/>
    /// </summary>
    internal static class GraphQlWsSocketWriterExtension
    {
        /// <summary>
        /// Writes a <see cref="GraphQlWsMessageType.Start"/> message to the writer
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
            writer.WriteType(GraphQlWsMessageType.Start);
            writer.WriteId(operationId);
            writer.WriteStartPayload();
            writer.Serialize(request);
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes a <see cref="GraphQlWsMessageType.Stop"/> message to the writer
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
            writer.WriteType(GraphQlWsMessageType.Stop);
            writer.WriteId(operationId);
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes a <see cref="GraphQlWsMessageType.ConnectionInit"/>message to the writer
        /// </summary>
        /// <param name="writer">The writer</param>
        public static void WriteInitializeMessage(this SocketMessageWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteType(GraphQlWsMessageType.ConnectionInit);
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes a <see cref="GraphQlWsMessageType.ConnectionTerminate"/>message to the writer
        /// </summary>
        /// <param name="writer">The writer</param>
        public static void WriteTerminateMessage(this SocketMessageWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteType(GraphQlWsMessageType.ConnectionTerminate);
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
            GraphQlWsMessageType type)
        {
            writer.Writer.WritePropertyName("type");
            ReadOnlySpan<byte> typeToWriter = type switch
            {
                GraphQlWsMessageType.ConnectionInit => ConnectionInitialize,
                GraphQlWsMessageType.ConnectionAccept => ConnectionAccept,
                GraphQlWsMessageType.ConnectionError => ConnectionError,
                GraphQlWsMessageType.KeepAlive => KeepAlive,
                GraphQlWsMessageType.ConnectionTerminate => ConnectionTerminate,
                GraphQlWsMessageType.Start => Start,
                GraphQlWsMessageType.Data => Data,
                GraphQlWsMessageType.Error => GraphQlWsMessageTypeSpans.Error,
                GraphQlWsMessageType.Complete => Complete,
                GraphQlWsMessageType.Stop => Stop,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
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
}
