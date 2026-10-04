namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Queues a message for a Codex thread through <c>codex queue</c>.
/// </summary>
internal interface ICodexQueueClient
{
    /// <summary>
    /// Returns the classified queue result without propagating process failures.
    /// Cancellation and timeout return <see cref="CodexQueueResult.Error"/>.
    /// </summary>
    Task<CodexQueueResult> QueueAsync(string threadId, string message, CancellationToken cancellationToken);
}
