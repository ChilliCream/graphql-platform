namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Handles opencode hook events without allowing hook failures to interrupt
/// the harness.
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
    /// Reserves unread mail for a later idle delivery when appropriate.
    /// </summary>
    Task<OpencodeHookOutcome> HandleSessionIdleAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the matching live session row.
    /// </summary>
    Task<OpencodeHookOutcome> HandleSessionDeletedAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken);
}
