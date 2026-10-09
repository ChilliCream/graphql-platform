namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Manages independent notification reservations per agent, message, and channel.
/// Reservations precede delivery and remain claimed until released.
/// </summary>
internal interface IAgentDeliveryLedger
{
    /// <summary>
    /// Returns input message ids with reservations across any channel for
    /// <paramref name="agent"/>, preserving input order. Empty input returns an empty result.
    /// </summary>
    Task<IReadOnlyList<string>> FindDeliveredAsync(
        string agent,
        IReadOnlyList<string> messageIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Claims previously unreserved messages for the agent and channel, returning
    /// new claims in input order without duplicates. Empty input returns an empty result.
    /// </summary>
    Task<IReadOnlyList<string>> ReserveAsync(
        string agent,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases the supplied message reservations for the agent and channel.
    /// Empty input changes nothing.
    /// </summary>
    Task ReleaseAsync(
        string agent,
        IReadOnlyList<string> messageIds,
        string channel,
        CancellationToken cancellationToken);
}
