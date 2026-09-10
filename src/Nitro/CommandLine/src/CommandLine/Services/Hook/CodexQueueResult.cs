namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// What a <c>codex queue</c> call resolved to.
/// </summary>
internal enum CodexQueueResult
{
    /// <summary>
    /// The subprocess exited zero: the message was durably queued.
    /// </summary>
    Ok,

    /// <summary>
    /// The subprocess reported that the thread no longer exists.
    /// </summary>
    EndpointGone,

    /// <summary>
    /// A spawn failure, a timeout, or any other nonzero exit that is not the
    /// gone-thread signature.
    /// </summary>
    Error
}
