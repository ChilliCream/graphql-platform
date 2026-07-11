namespace Mocha;

/// <summary>
/// Describes a message type dispatched by a saga lifecycle or transition.
/// </summary>
/// <param name="MessageType">The short type name of the dispatched message.</param>
/// <param name="MessageTypeFullName">The fully qualified type name, or <c>null</c> if unavailable.</param>
public sealed record SagaEventDescription(string MessageType, string? MessageTypeFullName);
