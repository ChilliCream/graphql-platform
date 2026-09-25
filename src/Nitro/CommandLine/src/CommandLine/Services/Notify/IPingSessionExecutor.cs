namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Performs a ping attempt within the supplied transport deadline.
/// Recording its outcome and releasing its transport slot are best effort.
/// </summary>
internal interface IPingSessionExecutor
{
    /// <summary>
    /// Returns a Codex ping outcome, reporting timeout without digest or transport work
    /// when the deadline has expired. Caller cancellation propagates; other attempt
    /// failures return an outcome, with best-effort recording and lease release.
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
    /// Returns a Claude peer ping outcome under the deadline, cancellation, and
    /// best-effort cleanup contract of <see cref="ExecuteCodexThreadAsync"/>.
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
    /// Pushes an opencode mail digest or performs a health check when no digest is available.
    /// Uses the deadline, cancellation, and best-effort recording and cleanup contract
    /// of <see cref="ExecuteCodexThreadAsync"/>.
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
