using System.Collections.Immutable;
using System.Threading.Channels;

namespace StrawberryShake;

/// <summary>
/// The entity store can be used to access and mutate entities.
/// </summary>
public partial class EntityStore
{
    private void BeginProcessEntityUpdates() =>
        Task<Task?>.Factory.StartNew(
            ProcessEntityUpdates,
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private async Task ProcessEntityUpdates()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested ||
                !_updates.Reader.Completion.IsCompleted)
            {
                var update = await _updates.Reader.ReadAsync(_cts.Token);
                _entityUpdateObservable.OnUpdated(update);
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
            _entityUpdateObservable.OnComplete();
        }
    }

    private sealed class EntityUpdateObservable : IObservable<EntityUpdate>
    {
        private readonly object _sync = new();
        private ImmutableList<IObserver<EntityUpdate>> _observers =
            ImmutableList<IObserver<EntityUpdate>>.Empty;

        public IDisposable Subscribe(IObserver<EntityUpdate> observer)
        {
            lock (_sync)
            {
                _observers = _observers.Add(observer);
            }

            return new Subscription(this, observer);
        }

        public void OnUpdated(EntityUpdate update)
        {
            var observers = _observers;

            if (observers.Count > 0)
            {
                foreach (var observer in observers)
                {
                    observer.OnNext(update);
                }
            }
        }

        public void OnComplete()
        {
            var observers = _observers;

            if (observers.Count > 0)
            {
                foreach (var observer in observers)
                {
                    observer.OnCompleted();
                }
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly EntityUpdateObservable _observable;
            private readonly IObserver<EntityUpdate> _observer;
            private bool _disposed;

            public Subscription(
                EntityUpdateObservable observable,
                IObserver<EntityUpdate> observer)
            {
                _observable = observable;
                _observer = observer;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    lock (_observable._sync)
                    {
                        _observable._observers = _observable._observers.Remove(_observer);
                    }
                    _disposed = true;
                }
            }
        }
    }
}
