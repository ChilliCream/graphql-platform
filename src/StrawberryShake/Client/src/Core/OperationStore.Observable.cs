using System.Collections.Immutable;
using System.Threading.Channels;

namespace StrawberryShake;

public sealed partial class OperationStore
{
    private void BeginProcessOperationUpdates(CancellationToken ct) =>
        Task<Task?>.Factory.StartNew(
            () => ProcessOperationUpdates(ct),
            ct,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private async Task ProcessOperationUpdates(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested || !_updates.Reader.Completion.IsCompleted)
            {
                var update = await _updates.Reader.ReadAsync(ct);
                _operationStoreObservable.Next(update);
            }
        }
        catch (ObjectDisposedException)
        {
            // we ignore disposed exceptions.
        }
        catch (OperationCanceledException)
        {
            // we ignore cancellation exceptions.
        }
        catch (ChannelClosedException)
        {
            // we ignore cancellation exceptions.
        }
        finally
        {
            // we complete the update queue and also send a complete signal to our observers.
            _updates.Writer.TryComplete();
            _operationStoreObservable.Complete();
        }
    }

    private sealed class OperationStoreObservable
        : IObservable<OperationUpdate>
        , IDisposable
    {
        private readonly object _sync = new();
        private ImmutableList<OperationStoreSession> _sessions =
            ImmutableList<OperationStoreSession>.Empty;

        public void Next(OperationUpdate operationUpdate)
        {
            var sessions = _sessions;

            foreach (var session in sessions)
            {
                session.Next(operationUpdate);
            }
        }

        public void Complete()
        {
            var sessions = _sessions;

            foreach (var session in sessions)
            {
                session.Complete();
            }
        }

        public IDisposable Subscribe(IObserver<OperationUpdate> observer)
        {
            var session = new OperationStoreSession(observer, Unsubscribe);

            lock (_sync)
            {
                _sessions = _sessions.Add(session);
            }

            return session;
        }

        private void Unsubscribe(OperationStoreSession session)
        {
            lock (_sync)
            {
                _sessions = _sessions.Remove(session);
            }
        }

        public void Dispose()
        {
            var sessions = _sessions;

            foreach (var session in sessions)
            {
                session.Complete();
                session.Dispose();
            }
        }

        private sealed class OperationStoreSession : IDisposable
        {
            private readonly IObserver<OperationUpdate> _observer;
            private readonly Action<OperationStoreSession> _unsubscribe;

            public OperationStoreSession(
                IObserver<OperationUpdate> observer,
                Action<OperationStoreSession> unsubscribe)
            {
                _observer = observer;
                _unsubscribe = unsubscribe;
            }

            public void Next(OperationUpdate operationUpdate)
            {
                _observer.OnNext(operationUpdate);
            }

            public void Complete()
            {
                _observer.OnCompleted();
            }

            public void Dispose()
            {
                _unsubscribe(this);
            }
        }
    }
}
