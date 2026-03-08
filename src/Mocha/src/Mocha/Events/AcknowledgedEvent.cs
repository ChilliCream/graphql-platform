namespace Mocha.Events;

/// <summary>
/// An internal event indicating successful acknowledgment of a request, completing the deferred response promise.
/// </summary>
/// <param name="CorrelationId">The correlation identifier linking this acknowledgment to the original request.</param>
/// <param name="MessageId">The message identifier of the original request, or <c>null</c> if not available.</param>
public sealed record AcknowledgedEvent(string CorrelationId, string? MessageId);
