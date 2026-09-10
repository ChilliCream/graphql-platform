namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// The deterministic outcome of one <see cref="TuiEffectQueue{TResult}"/> effect:
/// exactly one of a successful result, a thrown exception, or cooperative
/// cancellation. An effect never completes silently; every submission reaches
/// exactly one of these before it leaves the queue.
/// </summary>
internal abstract record TuiEffectCompletion<TResult>
{
    private TuiEffectCompletion()
    {
    }

    /// <summary>
    /// The operation ID assigned when the effect was submitted.
    /// </summary>
    public abstract TuiOperationId OperationId { get; init; }

    /// <summary>
    /// The effect ran to completion and produced <paramref name="Result"/>.
    /// </summary>
    public sealed record Completed(TuiOperationId OperationId, TResult Result) : TuiEffectCompletion<TResult>;

    /// <summary>
    /// The effect threw <paramref name="Exception"/> rather than completing normally.
    /// </summary>
    public sealed record Faulted(TuiOperationId OperationId, Exception Exception) : TuiEffectCompletion<TResult>;

    /// <summary>
    /// The effect observed cancellation before producing a result.
    /// </summary>
    public sealed record Cancelled(TuiOperationId OperationId) : TuiEffectCompletion<TResult>;
}
