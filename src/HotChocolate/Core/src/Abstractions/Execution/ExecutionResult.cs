namespace HotChocolate.Execution;

/// <summary>
/// A base-class for execution results that implements the dispose handling.
/// </summary>
public abstract class ExecutionResult : IExecutionResult
{
    private Func<ValueTask>[] _cleanupTasks = [];
    private bool _disposed;

    protected ExecutionResult()
    {
    }

    protected ExecutionResult(Func<ValueTask>[] cleanupTasks)
    {
        _cleanupTasks = cleanupTasks ?? throw new ArgumentNullException(nameof(cleanupTasks));
    }

    /// <inheritdoc cref="IExecutionResult" />
    public abstract ExecutionResultKind Kind { get; }

    /// <inheritdoc cref="IExecutionResult" />
    public abstract IReadOnlyDictionary<string, object?>? ContextData { get; }

    private protected Func<ValueTask>[] CleanupTasks => _cleanupTasks;

    /// <inheritdoc cref="IExecutionResult" />
    public void RegisterForCleanup(Func<ValueTask> clean)
    {
        if (clean is null)
        {
            throw new ArgumentNullException(nameof(clean));
        }

        var length = _cleanupTasks.Length;
        Array.Resize(ref _cleanupTasks, length + 1);
        _cleanupTasks[length] = clean;
    }

    /// <summary>
    /// Will throw an <see cref="ObjectDisposedException"/> if the result is already disposed.
    /// </summary>
    protected void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ResponseStream));
        }
    }

    /// <inheritdoc cref="IAsyncDisposable"/>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            for (var i = 0; i < _cleanupTasks.Length; i++)
            {
                await _cleanupTasks[i].Invoke();
            }

            _disposed = true;
        }
    }
}
