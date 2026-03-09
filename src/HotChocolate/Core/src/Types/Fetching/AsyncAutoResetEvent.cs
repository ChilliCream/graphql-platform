using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HotChocolate.Fetching;

internal sealed class AsyncAutoResetEvent : INotifyCompletion
{
    private readonly object _sync = new();
    private Action? _continuation;
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
            ThreadPool.QueueUserWorkItem(static c => c(), continuation, preferLocal: true);
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
            ThreadPool.QueueUserWorkItem(static c => c(), continuation, preferLocal: true);
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
