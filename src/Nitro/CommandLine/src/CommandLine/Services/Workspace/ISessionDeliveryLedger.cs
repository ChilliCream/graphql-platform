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
    /// Atomically claims each of <paramref name="messageIds"/> for
    /// <paramref name="channel"/> on the session identified by <paramref
    /// name="harness"/> and <paramref name="sessionId"/>. Returns the subset
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

    /// <summary>
    /// Same claim as the <c>(harness, sessionId, ...)</c> overload, but also
    /// conditioned on <paramref name="generation"/>'s host: a session row
    /// for the same harness and session id recorded by a different host
    /// reserves nothing. Used where more than one host can race the same
    /// session identity.
    /// </summary>
    Task<IReadOnlyList<string>> ReserveAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes each of <paramref name="messageIds"/>' reservations for
    /// <paramref name="channel"/> on the session identified by <paramref
    /// name="generation"/>, confined to its host exactly as the <see
    /// cref="ReserveAsync(AgentSessionGeneration, IReadOnlyList{string}, string, DateTimeOffset, CancellationToken)"/>
    /// overload reserves them: a session row recorded by a different host
    /// releases nothing. Frees an already-reserved message so a later call
    /// can reserve and deliver it again. An empty input is a no-op that
    /// opens no connection.
    /// </summary>
    Task ReleaseAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        CancellationToken cancellationToken);
}
