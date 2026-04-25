namespace Mocha;

/// <summary>
/// Describes a response event type associated with a saga state.
/// </summary>
/// <param name="EventType">The short type name of the response event.</param>
/// <param name="EventTypeFullName">The fully qualified type name, or <c>null</c> if unavailable.</param>
internal sealed record SagaResponseDescription(string EventType, string? EventTypeFullName);
