namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Handles Codex session registration, unread-mail context, session removal, and
/// notify delivery. Exceptions propagate to the hook executor.
/// </summary>
internal interface ICodexHookHandler
{
    /// <summary>
    /// Registers the session and returns its actor context.
    /// </summary>
    Task<CodexHookOutcome> HandleSessionStartAsync(
        CodexHookPayload payload, CancellationToken cancellationToken);

    /// <summary>
    /// Returns an unread-mail digest or count reminder for
    /// newly reserved messages in the current inbox batch, or a neutral outcome when
    /// no context is available.
    /// </summary>
    Task<CodexHookOutcome> HandleUserPromptSubmitAsync(
        CodexHookPayload payload, CancellationToken cancellationToken);

    /// <summary>
    /// Conditionally deletes the session's presence row.
    /// </summary>
    Task<CodexHookOutcome> HandleSessionEndAsync(
        CodexHookPayload payload, CancellationToken cancellationToken);

    /// <summary>
    /// The idle-turn gate: resolves the workspace from <paramref name="payload"/>'s
    /// <c>cwd</c>, matches the session row by thread id, and queues one digest via
    /// <c>codex queue --thread</c> for unread messages not yet claimed on the gate
    /// channel. A message already claimed is not re-queued.
    /// </summary>
    Task<CodexNotifyOutcome> HandleNotifyAsync(
        CodexNotifyPayload payload, CancellationToken cancellationToken);
}
