using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HotChocolate.Execution.Processing
{
    internal partial class WorkScheduler
    {
        private sealed class Pause : INotifyCompletion
        {
            private readonly object _sync;
            private Action? _continuation;
            private bool _continue;

            public Pause(object syncRoot)
            {
                _sync = syncRoot;
            }

            public bool IsCompleted => false;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                lock (_sync)
                {
                    if (_continue)
                    {
                        _continue = false;
                        continuation();
                        return;
                    }

                    if (_continuation is not null)
                    {
                        Debug.Assert(false, "We should not have to awaiter,");
                    }

                    _continuation = continuation;
                }
            }

            public void TryContinueUnsafe()
            {
                lock (_sync)
                {
                    Action? continuation = _continuation;
                    _continuation = null;

                    if (continuation is not null)
                    {
                        continuation.Invoke();
                        return;
                    }

                    _continue = true;
                }
            }

            public Pause GetAwaiter() => this;
        }
    }
}
