namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The at-most-once-per-channel notification ledger backed by
/// <c>session_deliveries</c>. Reserve-then-emit: a caller only emits the
/// messages whose reservation succeeded, so a crash between reserving and
/// actually emitting suppresses that message on that channel from then on,
/// but never suppresses it on a different channel or from a direct inbox
/// read.
/// </summary>
internal interface ISessionDeliveryLedger
{
    /// <summary>
    /// Returns the message ids from <paramref name="messageIds"/> that have
    /// been delivered to <paramref name="generation"/>, across all channels.
    /// The returned ids retain the input order, and an empty input returns an empty result.
    /// </summary>
    Task<IReadOnlyList<string>> FindDeliveredAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        CancellationToken cancellationToken)
        => Task.FromException<IReadOnlyList<string>>(new NotSupportedException());

    /// <summary>
    /// Atomically claims each of <paramref name="messageIds"/> for
    /// <paramref name="channel"/> on the given session. Returns the subset
    /// that was newly claimed by this call, in the order given; a message id
    /// already reserved for this session and channel (by this call or an
    /// earlier one) is silently excluded, never reserved twice. An empty
    /// input returns an empty result without opening a connection.
    /// </summary>
    Task<IReadOnlyList<string>> ReserveAsync(
        string harness,
        string sessionId,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken);
}
