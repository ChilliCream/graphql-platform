namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Injects a digest into a Codex thread via
/// <c>codex queue --thread &lt;id&gt; --message &lt;text&gt;</c>, a durable
/// cross-process write decoupled from
/// any live Codex process. The message is delivered as an extra prepended
/// user-message item ahead of whatever the thread's next actual turn is,
/// sharing that turn's single <c>notify</c> firing.
/// </summary>
internal interface ICodexQueueClient
{
    /// <summary>
    /// Runs the <c>codex queue</c> subprocess and classifies its outcome.
    /// Never throws: a spawn failure, a nonzero exit, or a timeout all
    /// return <see cref="CodexQueueResult.Error"/> (or
    /// <see cref="CodexQueueResult.EndpointGone"/> for the gone-thread
    /// signature) - fail-open, matching every other adapter member in this
    /// namespace. The ledger reservation this call's caller already made
    /// stands regardless (the plan's documented reserve-then-emit crash
    /// policy: a queue call lost after reservation suppresses that message
    /// on the gate channel, never a duplicate).
    /// </summary>
    Task<CodexQueueResult> QueueAsync(string threadId, string message, CancellationToken cancellationToken);
}
