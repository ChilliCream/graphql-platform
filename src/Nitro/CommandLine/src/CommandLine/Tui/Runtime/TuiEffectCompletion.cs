namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// The result, exception, or cancellation of a finished
/// <see cref="TuiEffectQueue{TResult}"/> effect.
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
    /// The effect threw an <see cref="OperationCanceledException"/>.
    /// </summary>
    public sealed record Cancelled(TuiOperationId OperationId) : TuiEffectCompletion<TResult>;
}
