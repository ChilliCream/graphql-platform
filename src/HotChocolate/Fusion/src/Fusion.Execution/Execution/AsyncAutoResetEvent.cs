using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HotChocolate.Fusion.Execution;

internal sealed class AsyncAutoResetEvent : INotifyCompletion, IThreadPoolWorkItem
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private Action? _continuation;
    private Action? _scheduledContinuation;
    private ExecutionContext? _scheduledContext;
    private bool _isSignaled;

    public bool IsSignaled => Volatile.Read(ref _isSignaled);

    public bool IsCompleted => false;

    public void GetResult() { }

    public void OnCompleted(Action continuation)
    {
        bool wasSignaled;

        lock (_sync)
        {
            wasSignaled = _isSignaled;

            if (wasSignaled)
            {
                // consume the signal
                _isSignaled = false;
            }
            else
            {
                Debug.Assert(_continuation is null, "There should only be one awaiter.");
                _continuation = continuation;
            }
        }

        if (wasSignaled)
        {
            ScheduleContinuation(continuation);
        }
    }

    public void Set()
    {
        Action? continuation = null;

        lock (_sync)
        {
            if (_continuation is not null)
            {
                // someone is waiting - release them immediately
                // we don't set _isSignaled since we're consuming it immediately
                continuation = _continuation;
                _continuation = null;
            }
            else
            {
                // since no one waiting we are storing the signal for the next awaiter
                _isSignaled = true;
            }
        }

        if (continuation is not null)
        {
            ScheduleContinuation(continuation);
        }
    }

    private void ScheduleContinuation(Action continuation)
    {
        Debug.Assert(_scheduledContinuation is null, "There should only be one scheduled awaiter.");
        _scheduledContinuation = continuation;
        _scheduledContext = ExecutionContext.Capture();
        ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
    }

    void IThreadPoolWorkItem.Execute()
    {
        var continuation = _scheduledContinuation!;
        var context = _scheduledContext;
        _scheduledContinuation = null;
        _scheduledContext = null;

        if (context is null)
        {
            continuation();
        }
        else
        {
            ExecutionContext.Run(context, static state => ((Action)state!).Invoke(), continuation);
        }
    }

    /// <summary>
    /// Attempts to clear a stored signal without waking a waiter.
    /// Returns true if a stored signal was present and is now cleared.
    /// </summary>
    public bool TryResetToIdle()
    {
        lock (_sync)
        {
            if (_continuation is null && _isSignaled)
            {
                _isSignaled = false;
                return true;
            }
            return false;
        }
    }

    public AsyncAutoResetEvent GetAwaiter() => this;
}
