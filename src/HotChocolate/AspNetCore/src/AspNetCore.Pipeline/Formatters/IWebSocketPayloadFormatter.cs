using System.Buffers;

namespace HotChocolate.AspNetCore.Formatters;

/// <summary>
/// This interface specifies how a GraphQL result is formatted as a WebSocket payload.
/// </summary>
public interface IWebSocketPayloadFormatter
{
    /// <summary>
    /// Formats the <paramref name="result"/> into a WebSocket payload.
    /// </summary>
    /// <param name="result">
    /// The GraphQL operation result.
    /// </param>
    /// <param name="writer">
    /// The buffer writer that is used to write the payload.
    /// </param>
    void Format(OperationResult result, IBufferWriter<byte> writer);

    /// <summary>
    /// Formats the <paramref name="error"/> into a WebSocket payload.
    /// </summary>
    /// <param name="error">
    /// The GraphQL execution error.
    /// </param>
    /// <param name="writer">
    /// The buffer writer that is used to write the error.
    /// </param>
    void Format(IError error, IBufferWriter<byte> writer);

    /// <summary>
    /// Formats the <paramref name="errors"/> into a WebSocket payload.
    /// </summary>
    /// <param name="errors">
    /// The GraphQL execution errors.
    /// </param>
    /// <param name="writer">
    /// The buffer writer that is used to write the errors.
    /// </param>
    void Format(IReadOnlyList<IError> errors, IBufferWriter<byte> writer);

    /// <summary>
    /// Formats the <paramref name="extensions"/> into a WebSocket payload.
    /// </summary>
    /// <param name="extensions">
    /// The GraphQL extensions.
    /// </param>
    /// <param name="writer">
    /// The buffer writer that is used to write the extensions.
    /// </param>
    void Format(IReadOnlyDictionary<string, object?> extensions, IBufferWriter<byte> writer);
}
