namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Performs one already-leased <c>codex-thread</c> ping attempt: builds the
/// machine-generated digest envelope for the bound actor's unread mail,
/// fires it at the endpoint bounded by <see cref="PingPolicy.HardTimeout"/>,
/// writes the outcome, and always releases the lease slot, however the
/// attempt ends. Shared by the foreground <c>nitro agent ping</c> command
/// and the detached <c>ping-worker</c> the notifier spawns, so both go
/// through the exact same cooldown/lease/result-write contract.
/// </summary>
internal interface IPingSessionExecutor
{
    /// <summary>
    /// Returns the <c>last_ping_result</c> value this attempt wrote (the
    /// same value a caller could re-read from the row): <c>ok</c>,
    /// <c>timeout</c>, or <c>error</c>. Never throws.
    /// </summary>
    Task<string> ExecuteCodexThreadAsync(
        string harness,
        string sessionId,
        string actorName,
        string endpointAddr,
        string attemptId,
        int slot,
        CancellationToken cancellationToken);
}
