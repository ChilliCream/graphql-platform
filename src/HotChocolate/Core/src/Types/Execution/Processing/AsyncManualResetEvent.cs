using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HotChocolate.Execution.Processing;

internal sealed class AsyncManualResetEvent : INotifyCompletion
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private Action? _continuation;
    private bool _continue;

    public bool IsPaused => !_continue;

    public bool IsCompleted => false;

    public void GetResult() { }

    public void OnCompleted(Action continuation)
    {
        bool cont;

        lock (_sync)
        {
            // first we capture the current state.
            cont = _continue;

            if (!cont)
            {
                // it is expected that there is only one awaiter per pause.
                Debug.Assert(_continuation is null, "There should only be one awaiter.");
                _continuation = continuation;
            }
        }

        // if we already received a continuation signal we will immediately
        // invoke the continuation delegate.
        if (cont)
        {
            continuation();
        }
    }

    public void Set()
    {
        Action? continuation;

        lock (_sync)
        {
            continuation = _continuation;
            _continue = true;
            _continuation = null;
        }

        if (continuation is not null)
        {
            ThreadPool.QueueUserWorkItem(c => c(), continuation, true);
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _continuation = null;
            _continue = false;
        }
    }

    public AsyncManualResetEvent GetAwaiter() => this;
}
