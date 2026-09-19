namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Injects a digest into a Codex thread via
/// <c>codex queue --thread &lt;id&gt; --message &lt;text&gt;</c>. The message is
/// delivered as an extra prepended user-message item ahead of the thread's next
/// actual turn.
/// </summary>
internal interface ICodexQueueClient
{
    /// <summary>
    /// Runs the <c>codex queue</c> subprocess and classifies its outcome. Never
    /// throws: a spawn failure, a nonzero exit, or a timeout all return
    /// <see cref="CodexQueueResult.Error"/>, or <see cref="CodexQueueResult.EndpointGone"/>
    /// for the gone-thread signature.
    /// </summary>
    Task<CodexQueueResult> QueueAsync(string threadId, string message, CancellationToken cancellationToken);
}
