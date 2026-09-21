namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Manages independent notification reservations per session, message, and channel.
/// Reservations precede delivery and remain claimed until released or cleared with session state.
/// </summary>
internal interface ISessionDeliveryLedger
{
    /// <summary>
    /// Returns input message ids with reservations across any channel for the supplied
    /// generation's harness and session id, preserving input order. Host is not part of
    /// the lookup; empty input returns an empty result.
    /// </summary>
    Task<IReadOnlyList<string>> FindDeliveredAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        CancellationToken cancellationToken)
        => Task.FromException<IReadOnlyList<string>>(new NotSupportedException());

    /// <summary>
    /// Claims previously unreserved messages for the session and channel, returning
    /// new claims in input order without duplicates. Empty input returns an empty result.
    /// </summary>
    Task<IReadOnlyList<string>> ReserveAsync(
        string harness,
        string sessionId,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Claims messages for the session and channel only when its host matches
    /// <paramref name="generation"/>. Returns new claims in input order, or an empty
    /// result for empty input or a missing or differently owned session.
    /// </summary>
    Task<IReadOnlyList<string>> ReserveAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases the supplied message reservations for the channel only when the
    /// session host matches <paramref name="generation"/>. Empty input or a missing
    /// or differently owned session changes nothing.
    /// </summary>
    Task ReleaseAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        CancellationToken cancellationToken);
}
