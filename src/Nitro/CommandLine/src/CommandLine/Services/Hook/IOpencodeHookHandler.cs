namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Handles opencode session lifecycle and prompt-context events.
/// Exceptions propagate to the hook executor.
/// </summary>
internal interface IOpencodeHookHandler
{
    /// <summary>
    /// Registers the newly created session and captures its endpoint details.
    /// </summary>
    Task<OpencodeHookOutcome> HandleSessionCreatedAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Returns context parts for a prompt and refreshes the session heartbeat.
    /// </summary>
    Task<OpencodeHookOutcome> HandleChatMessageAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes the session heartbeat.
    /// </summary>
    Task<OpencodeHookOutcome> HandleSessionIdleAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the matching live session row.
    /// </summary>
    Task<OpencodeHookOutcome> HandleSessionDeletedAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken);
}
