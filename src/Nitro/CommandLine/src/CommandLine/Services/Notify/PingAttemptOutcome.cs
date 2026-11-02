namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// A ping outcome with its result, reason, retry hint, actor, attempt, and completion time.
/// <see cref="Detail"/> is diagnostic text from the transport or a caught exception,
/// truncated to at most 200 characters; recording the outcome is best effort.
/// </summary>
internal sealed record PingAttemptOutcome(
    string Result,
    PingAttemptReason Reason,
    bool Retryable,
    string? Detail,
    string ActorName,
    string AttemptId,
    DateTimeOffset CompletedAt);
