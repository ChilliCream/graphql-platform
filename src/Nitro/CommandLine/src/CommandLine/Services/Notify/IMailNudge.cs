namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Attempts to notify recipient sessions of unread mail.
/// </summary>
internal interface IMailNudge
{
    /// <summary>
    /// Attempts nudges through Claude peer and Codex thread endpoints from registered
    /// session rows without checking liveness. Cancellation propagates; other failures
    /// are ignored.
    /// </summary>
    Task NudgeAsync(IReadOnlyList<string> actors, CancellationToken cancellationToken);
}
