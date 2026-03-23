using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HotChocolate.Fetching;

internal sealed class AsyncAutoResetEvent : INotifyCompletion
{
    private const int Idle = 0;
    private const int Signaled = 1;
    private const int Waiting = 2;

    private int _state;
    private Action? _continuation;

    public bool IsSignaled => Volatile.Read(ref _state) == Signaled;

    public bool IsCompleted => Volatile.Read(ref _state) == Signaled;

    public void GetResult()
    {
        // Consume the signal when completing synchronously (via IsCompleted == true).
        // CAS failure is benign (e.g. TryResetToIdle cleared it first).
        Interlocked.CompareExchange(ref _state, Idle, Signaled);
    }

    public void OnCompleted(Action continuation)
    {
        Debug.Assert(_continuation is null, "There should only be one awaiter.");
        _continuation = continuation;

        while (true)
        {
            switch (Volatile.Read(ref _state))
            {
                case Idle:
                    // Register waiter: IDLE -> WAITING
                    if (Interlocked.CompareExchange(ref _state, Waiting, Idle) == Idle)
                    {
                        return;
                    }
                    break; // CAS failed, retry

                case Signaled:
                    // Consume signal immediately: SIGNALED -> IDLE
                    if (Interlocked.CompareExchange(ref _state, Idle, Signaled) == Signaled)
                    {
                        _continuation = null;
                        ThreadPool.QueueUserWorkItem(static c => c(), continuation, preferLocal: true);
                        return;
                    }
                    break; // CAS failed, retry

                default:
                    Debug.Fail("OnCompleted called while already waiting.");
                    return;
            }
        }
    }

    public void Set()
    {
        while (true)
        {
            switch (Volatile.Read(ref _state))
            {
                case Idle:
                    // Store signal: IDLE -> SIGNALED
                    if (Interlocked.CompareExchange(ref _state, Signaled, Idle) == Idle)
                    {
                        return;
                    }
                    break; // CAS failed, retry

                case Waiting:
                    // Wake waiter: WAITING -> IDLE
                    if (Interlocked.CompareExchange(ref _state, Idle, Waiting) == Waiting)
                    {
                        var c = _continuation!;
                        _continuation = null;
                        ThreadPool.QueueUserWorkItem(static c => c(), c, preferLocal: true);
                        return;
                    }
                    break; // CAS failed, retry

                case Signaled:
                    // Already signaled, nothing to do
                    return;

                default:
                    return;
            }
        }
    }

    /// <summary>
    /// Attempts to clear a stored signal without waking a waiter.
    /// Returns true if a stored signal was present and is now cleared.
    /// </summary>
    public bool TryResetToIdle()
    {
        return Interlocked.CompareExchange(ref _state, Idle, Signaled) == Signaled;
    }

    public AsyncAutoResetEvent GetAwaiter() => this;
}
