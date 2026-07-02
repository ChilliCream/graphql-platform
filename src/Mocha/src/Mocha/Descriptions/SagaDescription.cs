namespace Mocha;

/// <summary>
/// Describes a saga state machine for diagnostic and visualization purposes.
/// </summary>
/// <param name="Id">The stable URN identity of this saga.</param>
/// <param name="Name">The logical name of the saga.</param>
/// <param name="StateType">The short type name of the saga state.</param>
/// <param name="StateTypeFullName">The fully qualified type name of the saga state, or <c>null</c> if unavailable.</param>
/// <param name="ConsumerName">The name of the consumer that drives this saga.</param>
/// <param name="States">The descriptions of all states in this saga.</param>
public sealed record SagaDescription(
    string Id,
    string Name,
    string StateType,
    string? StateTypeFullName,
    string ConsumerName,
    IReadOnlyList<SagaStateDescription> States);
