using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HotChocolate.Execution.Processing;

internal partial class WorkScheduler
{
    private sealed class ProcessingPause : INotifyCompletion
    {
        private readonly object _sync = new();
        private Action? _continuation;
        private bool _continue;

        public bool IsCompleted => false;

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            lock (_sync)
            {
                // if we already received a continuation signal we can immediately
                // continue without delay.
                if (_continue)
                {
                    continuation();
                    return;
                }

                // it is expected that there is only one awaiter per pause.
                Debug.Assert(
                    _continuation is null, 
                    "There should only be one awaiter.");
                _continuation = continuation;
            }
        }

        public void TryContinue()
        {
            lock (_sync)
            {
                if (_continuation is not null)
                {
                    _continuation();
                    return;
                }

                _continuation = null;
                _continue = true;
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

        public ProcessingPause GetAwaiter() => this;
    }
}
