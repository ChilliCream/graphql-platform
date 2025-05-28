namespace HotChocolate.Execution;

/// <summary>
/// Specifies the kind of execution result.
/// </summary>
public enum ExecutionResultKind
{
    /// <summary>
    /// A single result.
    /// </summary>
    SingleResult,

    /// <summary>
    /// A deferred response stream.
    /// </summary>
    DeferredResult,

    /// <summary>
    /// A batch response stream.
    /// </summary>
    BatchResult,

    /// <summary>
    /// A subscription response stream.
    /// </summary>
    SubscriptionResult,

    /// <summary>
    /// A no-op result for warmup requests.
    /// </summary>
    WarmupResult,
}
