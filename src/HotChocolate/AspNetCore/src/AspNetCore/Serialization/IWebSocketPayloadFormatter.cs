using System.Text.Json;

namespace HotChocolate.AspNetCore.Serialization;

/// <summary>
/// This interface specifies how a GraphQL result is formatted as a WebSocket payload.
/// </summary>
public interface IWebSocketPayloadFormatter
{
    /// <summary>
    /// Formats the given <paramref name="result"/> into a WebSocket payload.
    /// </summary>
    /// <param name="result">
    /// The GraphQL operation result.
    /// </param>
    /// <param name="jsonWriter">
    /// The JSON writer that is used to write the payload.
    /// </param>
    void Format(IOperationResult result, Utf8JsonWriter jsonWriter);

    /// <summary>
    /// Formats the given <paramref name="error"/> into a WebSocket payload.
    /// </summary>
    /// <param name="error">
    /// The GraphQL execution error.
    /// </param>
    /// <param name="jsonWriter">
    /// The JSON writer that is used to write the error.
    /// </param>
    void Format(IError error, Utf8JsonWriter jsonWriter);

    /// <summary>
    /// Formats the given <paramref name="errors"/> into a WebSocket payload.
    /// </summary>
    /// <param name="errors">
    /// The GraphQL execution errors.
    /// </param>
    /// <param name="jsonWriter">
    /// The JSON writer that is used to write the errors.
    /// </param>
    void Format(IReadOnlyList<IError> errors, Utf8JsonWriter jsonWriter);
}
