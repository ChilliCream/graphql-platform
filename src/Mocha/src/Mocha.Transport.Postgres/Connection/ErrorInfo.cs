using System.Text.Json.Serialization;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Represents error information captured when a message processing attempt fails.
/// Accumulated as a JSONB array in the <c>error_reason</c> column of the message table.
/// </summary>
public sealed record ErrorInfo(
    [property: JsonPropertyName("type")] string ExceptionType,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("stackTrace")] string? StackTrace)
{
    /// <summary>
    /// Creates an <see cref="ErrorInfo"/> from an exception.
    /// </summary>
    public static ErrorInfo From(Exception exception)
        => new(exception.GetType().Name, exception.Message, exception.StackTrace);
}
