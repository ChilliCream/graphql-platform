using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

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

                    Debug.Assert(_continuation is null, "There should only be one awaiter.");
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
