namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The typed result of one already-leased <see cref="IPingSessionExecutor"/>
/// attempt: <see cref="Result"/> is the coarse, CHECK-compatible value also
/// durably written to <c>agent_sessions.last_ping_result</c>; <see cref="Reason"/>
/// and <see cref="Retryable"/> give the caller a stable, more specific
/// classification without changing what gets persisted. <see cref="Detail"/>
/// mirrors <c>agent_sessions.last_ping_detail</c>: bounded and safe to log,
/// never a raw exception message or subprocess stderr.
/// </summary>
internal sealed record PingAttemptOutcome(
    string Result,
    PingAttemptReason Reason,
    bool Retryable,
    string? Detail,
    string Harness,
    string SessionId,
    string AttemptId,
    DateTimeOffset CompletedAt);
