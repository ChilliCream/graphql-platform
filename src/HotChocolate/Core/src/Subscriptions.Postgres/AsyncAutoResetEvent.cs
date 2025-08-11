namespace HotChocolate.Subscriptions.Postgres;

/// <summary>
/// This is a very similar implementation to the AsyncAutoResetEvent from Stephen Toub's blog post:
/// https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-2-asyncautoresetevent/
/// </summary>
internal sealed class AsyncResetEvent : IDisposable
{
    private static readonly Task s_completedTask = Task.FromResult(true);

    private readonly Queue<TaskCompletionSource<bool>> _waitingTasks = new();
    private bool _signaled;

    private bool _isDisposed;

    public Task WaitAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        lock (_waitingTasks)
        {
            if (_signaled)
            {
                _signaled = false;
                return s_completedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            _waitingTasks.Enqueue(tcs);
            return tcs.Task.WaitAsync(cancellationToken);
        }
    }

    public void Set()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        TaskCompletionSource<bool>? toRelease = null;
        lock (_waitingTasks)
        {
            while (_waitingTasks.TryDequeue(out var task))
            {
                if (task.Task.IsCanceled)
                {
                    continue;
                }

                toRelease = task;
                break;
            }

            if (toRelease is null && !_signaled)
            {
                _signaled = true;
            }
        }

        toRelease?.SetResult(true);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_waitingTasks)
        {
            foreach (var wait in _waitingTasks)
            {
                wait.TrySetCanceled();
            }
        }

        _isDisposed = true;
    }
}
