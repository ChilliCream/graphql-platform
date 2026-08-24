namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The stable reasons one <see cref="IPingSessionExecutor"/> attempt can
/// end in, spanning both the Claude peer and Codex thread transports.
/// </summary>
internal enum PingAttemptReason
{
    Ok,
    Unsupported,
    EndpointGone,

    /// <summary>
    /// The Claude peer endpoint required authentication this process could
    /// not supply, or the matching key was missing or invalid.
    /// </summary>
    InvalidAuth,

    /// <summary>
    /// The Claude peer socket connect failed with <c>SocketError.AccessDenied</c>.
    /// </summary>
    AccessDenied,
    Timeout,
    CapacityDropped,

    /// <summary>
    /// Any other transport failure: a Codex spawn failure or nonzero exit,
    /// a generic Claude peer I/O or protocol failure, or an unexpected
    /// exception this attempt caught.
    /// </summary>
    TransportError
}

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
