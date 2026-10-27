namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// What a <c>codex queue</c> call resolved to.
/// </summary>
internal enum CodexQueueResult
{
    /// <summary>
    /// The queue subprocess reported success.
    /// </summary>
    Ok,

    /// <summary>
    /// The subprocess reported that the thread no longer exists.
    /// </summary>
    EndpointGone,

    /// <summary>
    /// A queue failure, timeout, or cancellation without a gone-thread result.
    /// </summary>
    Error
}
