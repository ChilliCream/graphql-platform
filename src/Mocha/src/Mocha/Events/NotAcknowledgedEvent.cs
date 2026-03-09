namespace Mocha.Events;

/// <summary>
/// An internal event indicating that a request was not acknowledged, carrying an error code and optional details.
/// </summary>
/// <param name="CorrelationId">The correlation identifier of the failed request, or <c>null</c>.</param>
/// <param name="MessageId">The message identifier of the failed request, or <c>null</c>.</param>
/// <param name="ErrorCode">A well-known error code from <see cref="ErrorCodes"/>.</param>
/// <param name="ErrorMessage">A human-readable description of the error, or <c>null</c>.</param>
public sealed record NotAcknowledgedEvent(
    string? CorrelationId,
    string? MessageId,
    string ErrorCode,
    string? ErrorMessage);
