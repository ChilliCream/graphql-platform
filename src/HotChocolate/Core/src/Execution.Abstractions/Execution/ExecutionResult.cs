using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Features;

namespace HotChocolate.Execution;

/// <summary>
/// A base-class for execution results that implements the dispose handling.
/// </summary>
public abstract class ExecutionResult : IExecutionResult
{
    private static readonly ArrayPool<CleanupEntry> s_cleanUpTaskPool = ArrayPool<CleanupEntry>.Shared;
    private CleanupEntry[] _cleanUpTasks = [];
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

    /// <summary>
    /// Registers a resource that needs to be disposed when the result is being disposed.
    /// </summary>
    /// <param name="disposable">
    /// The resource that needs to be disposed.
    /// </param>
    public void RegisterForCleanup(IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        AddCleanupEntry(new CleanupEntry { Target = disposable, Kind = CleanupKind.Disposable });
    }

    /// <summary>
    /// Registers a cleanup action to be executed when the result is disposed.
    /// </summary>
    /// <param name="clean">
    /// A cleanup action that will be executed when this result is disposed.
    /// </param>
    public void RegisterForCleanup(Action clean)
    {
        ArgumentNullException.ThrowIfNull(clean);
        AddCleanupEntry(new CleanupEntry { Target = clean, Kind = CleanupKind.Action });
    }

    /// <summary>
    /// Registers a resource that needs to be disposed asynchronously when the result is being disposed.
    /// </summary>
    /// <param name="disposable">
    /// The resource that needs to be disposed.
    /// </param>
    public void RegisterForCleanup(IAsyncDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        AddCleanupEntry(new CleanupEntry { Target = disposable, Kind = CleanupKind.AsyncDisposable });
    }

    /// <inheritdoc cref="IExecutionResult" />
    public void RegisterForCleanup(Func<ValueTask> clean)
    {
        ArgumentNullException.ThrowIfNull(clean);
        AddCleanupEntry(new CleanupEntry { Target = clean, Kind = CleanupKind.FuncValueTask });
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
                var entries = _cleanUpTasks;

                for (var i = 0; i < _cleanupTasksLength; i++)
                {
                    switch (entries[i].Kind)
                    {
                        case CleanupKind.FuncValueTask:
                            await Unsafe.As<Func<ValueTask>>(entries[i].Target).Invoke().ConfigureAwait(false);
                            break;

                        case CleanupKind.Disposable:
                            Unsafe.As<IDisposable>(entries[i].Target).Dispose();
                            break;

                        case CleanupKind.AsyncDisposable:
                            await Unsafe.As<IAsyncDisposable>(entries[i].Target).DisposeAsync().ConfigureAwait(false);
                            break;

                        case CleanupKind.Action:
                            Unsafe.As<Action>(entries[i].Target).Invoke();
                            break;
                    }
                }

                entries.AsSpan(0, _cleanupTasksLength).Clear();
                s_cleanUpTaskPool.Return(entries);
            }

            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    private void AddCleanupEntry(CleanupEntry entry)
    {
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

        _cleanUpTasks[_cleanupTasksLength++] = entry;
    }

    private enum CleanupKind
    {
        FuncValueTask = 0,
        Disposable = 1,
        AsyncDisposable = 2,
        Action = 3
    }

    private struct CleanupEntry
    {
        public object Target;
        public CleanupKind Kind;
    }
}
