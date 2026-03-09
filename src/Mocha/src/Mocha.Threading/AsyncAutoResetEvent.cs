using static System.Threading.Tasks.TaskContinuationOptions;

namespace Mocha.Threading;

/// <summary>
/// This is a very similar implementation to the AsyncAutoResetEvent from Stephen Toub's blog post:
/// https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-2-asyncautoresetevent/
/// </summary>
public sealed class AsyncAutoResetEvent(bool releaseAllOnSet = false) : IDisposable
{
    private static readonly Task s_completedTask = Task.FromResult(true);

    private readonly Queue<TaskCompletionSource<bool>> _waitingTasks = new();
    private bool _signaled;

    private bool _isDisposed;

    /// <summary>
    /// Asynchronously waits for the event to be signaled, returning a task that completes when
    /// the signal is received or the token is canceled.
    /// </summary>
    /// <remarks>
    /// If the event is already in a signaled state, the call returns immediately and resets the
    /// signal. Otherwise the caller is enqueued and resumed when <see cref="Set"/> is called.
    /// </remarks>
    /// <param name="cancellationToken">A token to cancel the wait. Cancellation causes the returned task to transition to <c>Canceled</c>.</param>
    /// <returns>A task that completes when the event is signaled or the wait is canceled.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the event has been disposed.</exception>
    public Task WaitAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(AsyncAutoResetEvent));

        lock (_waitingTasks)
        {
            if (_signaled)
            {
                _signaled = false;
                return s_completedTask;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var registration = cancellationToken.Register(() => tcs.TrySetCanceled());

            if (cancellationToken.IsCancellationRequested)
            {
                registration.Dispose();
                tcs.TrySetCanceled();
                return tcs.Task;
            }

            _waitingTasks.Enqueue(tcs);

            tcs.Task.ContinueWith(_ => registration.Dispose(), ExecuteSynchronously);

            return tcs.Task;
        }
    }

    /// <summary>
    /// Signals the event, releasing one waiting task or, when <c>releaseAllOnSet</c> is enabled, all waiting tasks.
    /// </summary>
    /// <remarks>
    /// If no tasks are waiting and <c>releaseAllOnSet</c> is <c>false</c>, the event latches into
    /// a signaled state so the next <see cref="WaitAsync"/> call completes immediately.
    /// When <c>releaseAllOnSet</c> is <c>true</c>, all enqueued waiters are released and the
    /// event remains signaled.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the event has been disposed.</exception>
    public void Set()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(AsyncAutoResetEvent));

        if (!releaseAllOnSet)
        {
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
        else
        {
            lock (_waitingTasks)
            {
                while (_waitingTasks.TryDequeue(out var task))
                {
                    if (!task.Task.IsCanceled)
                    {
                        try
                        {
                            task.SetResult(true);
                        }
                        catch
                        {
                            // Ignore exceptions
                        }
                    }
                }

                _signaled = true;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_waitingTasks)
        {
            while (_waitingTasks.TryDequeue(out var wait))
            {
                wait.TrySetCanceled();
            }
        }

        _isDisposed = true;
    }
}
