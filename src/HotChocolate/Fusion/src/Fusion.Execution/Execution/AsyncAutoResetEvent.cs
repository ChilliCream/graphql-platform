using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// An auto-reset event with a single awaiter. A signal raised while no one awaits is stored
/// and completes the next await.
/// </summary>
internal sealed class AsyncAutoResetEvent : ICriticalNotifyCompletion, IThreadPoolWorkItem
{
    private const string SocketThreadCountVariable = "DOTNET_SYSTEM_NET_SOCKETS_THREAD_COUNT";

    private static readonly bool s_runInlineOutsideThreadPool = HasTwoSocketEnginesPerProcessor(
        Environment.GetEnvironmentVariable(SocketThreadCountVariable),
        Environment.ProcessorCount);

#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly bool _runInlineOutsideThreadPool;
    private Action? _continuation;
    private ExecutionContext? _continuationContext;
    private Action? _scheduledContinuation;
    private ExecutionContext? _scheduledContext;
    private bool _isSignaled;

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncAutoResetEvent"/> that runs a continuation on the
    /// thread that sets the event when that thread is not a thread pool thread and
    /// <c>DOTNET_SYSTEM_NET_SOCKETS_THREAD_COUNT</c> configures at least two socket engines per processor.
    /// </summary>
    public AsyncAutoResetEvent()
        : this(s_runInlineOutsideThreadPool)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncAutoResetEvent"/>.
    /// </summary>
    /// <param name="runInlineOutsideThreadPool">
    /// Whether a signal raised on a thread that is not a thread pool thread runs the continuation on
    /// that thread instead of queuing it to the thread pool.
    /// </param>
    internal AsyncAutoResetEvent(bool runInlineOutsideThreadPool)
    {
        _runInlineOutsideThreadPool = runInlineOutsideThreadPool;
    }

    public bool IsSignaled => Volatile.Read(ref _isSignaled);

    /// <summary>
    /// Gets whether a stored signal completes the current await. Reading it consumes the stored signal.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsCompleted
    {
        get
        {
            if (!Volatile.Read(ref _isSignaled))
            {
                return false;
            }

            lock (_sync)
            {
                if (_isSignaled)
                {
                    // consume the signal, the awaiter continues synchronously
                    _isSignaled = false;
                    return true;
                }
            }

            return false;
        }
    }

    public void GetResult() { }

    public void OnCompleted(Action continuation)
        => RegisterContinuation(continuation, ExecutionContext.Capture());

    public void UnsafeOnCompleted(Action continuation)
        => RegisterContinuation(continuation, context: null);

    /// <summary>
    /// Signals the event. A registered awaiter continues on the thread pool, or on the calling thread
    /// when inline runs are enabled and the caller is not a thread pool thread; without an awaiter the
    /// signal is stored.
    /// </summary>
    public void Set()
    {
        Action? continuation = null;
        ExecutionContext? context = null;

        lock (_sync)
        {
            if (_continuation is not null)
            {
                // someone is waiting - release them immediately
                // we don't set _isSignaled since we're consuming it immediately
                continuation = _continuation;
                context = _continuationContext;
                _continuation = null;
                _continuationContext = null;
            }
            else
            {
                // since no one waiting we are storing the signal for the next awaiter
                _isSignaled = true;
            }
        }

        if (continuation is null)
        {
            return;
        }

        if (_runInlineOutsideThreadPool && !Thread.CurrentThread.IsThreadPoolThread)
        {
            RunContinuation(continuation, context);
        }
        else
        {
            ScheduleContinuation(continuation, context);
        }
    }

    /// <summary>
    /// Gets whether <paramref name="threadCount"/>, the value of <c>DOTNET_SYSTEM_NET_SOCKETS_THREAD_COUNT</c>,
    /// configures at least two socket engines per processor. A missing or unparsable value returns false.
    /// </summary>
    internal static bool HasTwoSocketEnginesPerProcessor(string? threadCount, int processorCount)
        => uint.TryParse(threadCount, out var count) && count >= 2L * processorCount;

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

    void IThreadPoolWorkItem.Execute()
    {
        // The continuation can await this event again and schedule this instance
        // before it returns, so the slot must be free before we invoke it.
        var continuation = _scheduledContinuation;
        var context = _scheduledContext;
        _scheduledContinuation = null;
        _scheduledContext = null;

        if (continuation is null)
        {
            return;
        }

        RunContinuation(continuation, context);
    }

    private void RegisterContinuation(Action continuation, ExecutionContext? context)
    {
        bool wasSignaled;

        lock (_sync)
        {
            // a signal can arrive between the IsCompleted check and the registration
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
                _continuationContext = context;
            }
        }

        if (wasSignaled)
        {
            ScheduleContinuation(continuation, context);
        }
    }

    private static void RunContinuation(Action continuation, ExecutionContext? context)
    {
        if (context is null)
        {
            continuation();
        }
        else
        {
            ExecutionContext.Run(context, static state => ((Action)state!)(), continuation);
        }
    }

    private void ScheduleContinuation(Action continuation, ExecutionContext? context)
    {
        // The single awaiter registers again only after its continuation ran,
        // so at most one continuation is scheduled at any time.
        Debug.Assert(_scheduledContinuation is null, "There should only be one scheduled continuation.");
        _scheduledContinuation = continuation;
        _scheduledContext = context;
        ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
    }
}
