using System.Diagnostics;

namespace HotChocolate.Fetching;

public sealed partial class BatchDispatcher
{
    private class ExecutorSession : IDisposable
    {
        private readonly IObserver<BatchDispatchEventArgs> _observer;
        private readonly BatchDispatcher _dispatcher;
        private bool _disposed;

        public ExecutorSession(
            IObserver<BatchDispatchEventArgs> observer,
            BatchDispatcher dispatcher)
        {
            _observer = observer;
            _dispatcher = dispatcher;

            lock (dispatcher._sync)
            {
                dispatcher._sessions = dispatcher._sessions.Add(this);
                dispatcher._lastSubscribed = Stopwatch.GetTimestamp();
                dispatcher._signal.Set();
            }
        }

        public void OnNext(BatchDispatchEventArgs e)
        {
            _observer.OnNext(e);
        }

        public void OnCompleted()
        {
            _observer.OnCompleted();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_dispatcher._sync)
                {
                    _dispatcher._sessions = _dispatcher._sessions.Remove(this);
                }

                _disposed = true;
            }
        }
    }
}
