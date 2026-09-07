namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Performs one already-leased ping attempt: builds the
/// machine-generated digest envelope for the bound actor's unread mail,
/// fires it at the endpoint bounded by the caller's absolute deadline,
/// writes the outcome, and always releases the lease slot, however the
/// attempt ends. Used by automatic mail wake delivery.
/// </summary>
internal interface IPingSessionExecutor
{
    /// <summary>
    /// Runs one Codex thread attempt and returns the typed outcome this
    /// attempt wrote (the same coarse <see cref="PingAttemptOutcome.Result"/>
    /// a caller could re-read from the row: <c>ok</c>, <c>timeout</c>,
    /// <c>endpoint-gone</c>, or <c>error</c>). Never throws except
    /// <see cref="OperationCanceledException"/> when the caller's own
    /// <paramref name="cancellationToken"/> is cancelled; the lease slot is
    /// still guaranteed to be released in that case. <paramref name="deadline"/>
    /// is the absolute UTC instant this attempt's digest and transport work
    /// must finish before; a deadline already in the past records
    /// <c>timeout</c> without starting either.
    /// </summary>
    Task<PingAttemptOutcome> ExecuteCodexThreadAsync(
        string harness,
        string sessionId,
        string actorName,
        string endpointAddr,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken);

    /// <summary>
    /// Runs one Claude peer attempt and returns the typed outcome this
    /// attempt wrote, with the same never-throws and deadline contract as
    /// <see cref="ExecuteCodexThreadAsync"/>.
    /// </summary>
    Task<PingAttemptOutcome> ExecuteClaudePeerAsync(
        string harness,
        string sessionId,
        string actorName,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken);

    /// <summary>
    /// Runs one opencode server attempt and returns the typed outcome this
    /// attempt wrote, with the same never-throws and deadline contract as
    /// <see cref="ExecuteCodexThreadAsync"/>. Pushes the mail digest as a
    /// real prompt when unread mail remains, or performs a health-only ping
    /// against <paramref name="endpointAddr"/> when it does not, so a
    /// session with nothing left to deliver still surfaces a gone or timed
    /// out endpoint instead of a blind <c>ok</c>. Every health-only outcome
    /// is written to the row like any other attempt.
    /// </summary>
    Task<PingAttemptOutcome> ExecuteOpencodeServerAsync(
        string harness,
        string sessionId,
        string actorName,
        string endpointAddr,
        string? endpointSecret,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken);
}
