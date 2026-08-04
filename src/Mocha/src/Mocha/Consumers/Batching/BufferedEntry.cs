using static System.Threading.Tasks.TaskCreationOptions;

namespace Mocha;

/// <summary>
/// A buffered context with built-in pipeline coordination.
/// </summary>
internal readonly struct BufferedEntry<TEvent>(IConsumeContext<TEvent> context)
{
    private readonly TaskCompletionSource<bool> _completion = new(RunContinuationsAsynchronously);

    public IConsumeContext<TEvent> Context { get; } = context;

    /// <summary>
    /// The task that the per-message pipeline awaits. Completes when
    /// the batch handler finishes (success, fault, or cancellation).
    /// </summary>
    public Task Task => _completion.Task;

    public void Complete() => _completion.TrySetResult(true);

    public void Cancel() => _completion.TrySetCanceled();

    public void Fault(Exception exception) => _completion.TrySetException(exception);
}
