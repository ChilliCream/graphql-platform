using System.Buffers;
using System.Collections.Immutable;
using HotChocolate.Features;

namespace HotChocolate.Execution;

/// <summary>
/// A base-class for execution results that implements the dispose handling.
/// </summary>
public abstract class ExecutionResult : IExecutionResult
{
    private static readonly ArrayPool<Func<ValueTask>> s_cleanUpTaskPool = ArrayPool<Func<ValueTask>>.Shared;
    private Func<ValueTask>[] _cleanUpTasks = [];
    private int _cleanupTasksLength;
    private bool _disposed;

    /// <inheritdoc cref="IExecutionResult" />
    public abstract ExecutionResultKind Kind { get; }

    /// <inheritdoc cref="IExecutionResult" />
    public ImmutableDictionary<string, object?> ContextData
    {
        get => Features.Get<ImmutableDictionary<string, object?>>() ?? ImmutableDictionary<string, object?>.Empty;
        set => Features.Set(value);
    }

    /// <inheritdoc cref="IFeatureProvider" />
    public IFeatureCollection Features { get; } = new FeatureCollection();

    /// <inheritdoc cref="IExecutionResult" />
    public void RegisterForCleanup(Func<ValueTask> clean)
    {
        ArgumentNullException.ThrowIfNull(clean);

        if (_cleanUpTasks.Length == 0)
        {
            _cleanUpTasks = s_cleanUpTaskPool.Rent(8);
            _cleanupTasksLength = 0;
        }
        else if (_cleanupTasksLength >= _cleanUpTasks.Length)
        {
            var buffer = s_cleanUpTaskPool.Rent(_cleanupTasksLength * 2);
            var currentBuffer = _cleanUpTasks.AsSpan();

            currentBuffer.CopyTo(buffer);
            currentBuffer.Clear();
            s_cleanUpTaskPool.Return(_cleanUpTasks);

            _cleanUpTasks = buffer;
        }

        _cleanUpTasks[_cleanupTasksLength++] = clean;
    }

    /// <summary>
    /// Will throw an <see cref="ObjectDisposedException"/> if the result is already disposed.
    /// </summary>
    protected void EnsureNotDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    /// <inheritdoc cref="IAsyncDisposable"/>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_cleanupTasksLength > 0)
            {
                var tasks = _cleanUpTasks;

                for (var i = 0; i < _cleanupTasksLength; i++)
                {
                    await tasks[i]().ConfigureAwait(false);
                }

                tasks.AsSpan(0, _cleanupTasksLength).Clear();
                s_cleanUpTaskPool.Return(tasks);
            }

            _disposed = true;
        }
    }
}
