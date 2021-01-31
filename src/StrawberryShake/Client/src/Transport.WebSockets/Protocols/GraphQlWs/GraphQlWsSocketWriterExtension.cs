using StrawberryShake.Transport.Http;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.Transport
{
    /// <summary>
    /// Common extension of the <see cref="SocketMessageWriter"/> for
    /// <see cref="GraphQlWsProtocol"/>
    /// </summary>
    internal static class GraphQlWsSocketWriterExtension
    {
        /// <summary>
        /// Writes a <see cref="GraphQlWsMessageTypes.Operation.Start"/> message to the writer
        /// </summary>
        /// <param name="writer">The writer</param>
        /// <param name="operationId">The operation id of the operation</param>
        /// <param name="request">The operation request containing the payload</param>
        public static void WriteStartOperationMessage(
            this SocketMessageWriter writer,
            string operationId,
            OperationRequest request)
        {
            writer.WriteStartObject();
            writer.WriteType(GraphQlWsMessageTypes.Operation.Start);
            writer.WriteId(operationId);
            writer.WriteStartPayload();
            writer.Serialize(request);
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes a <see cref="GraphQlWsMessageTypes.Operation.Stop"/> message to the writer
        /// </summary>
        /// <param name="writer">The writer</param>
        /// <param name="operationId">The operation id of the operation</param>
        public static void WriteStopOperationMessage(
            this SocketMessageWriter writer,
            string operationId)
        {
            writer.WriteStartObject();
            writer.WriteType(GraphQlWsMessageTypes.Operation.Stop);
            writer.WriteId(operationId);
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes a <see cref="GraphQlWsMessageTypes.Connection.Initialize"/>message to the writer
        /// </summary>
        /// <param name="writer">The writer</param>
        public static void WriteInitializeMessage(this SocketMessageWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteType(GraphQlWsMessageTypes.Connection.Initialize);
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes a <see cref="GraphQlWsMessageTypes.Connection.Terminate"/>message to the writer
        /// </summary>
        /// <param name="writer">The writer</param>
        public static void WriteTerminateMessage(this SocketMessageWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteType(GraphQlWsMessageTypes.Connection.Terminate);
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
            string type)
        {
            writer.Writer.WritePropertyName("type");
            writer.Writer.WriteStringValue(type);
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
