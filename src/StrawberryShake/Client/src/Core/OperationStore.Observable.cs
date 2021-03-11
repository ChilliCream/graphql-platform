using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using StrawberryShake.Extensions;

namespace StrawberryShake
{
    public sealed partial class OperationStore
    {
        private void BeginProcessOperationUpdates() =>
            Task.Run(async () => await ProcessOperationUpdates());

        private async Task ProcessOperationUpdates()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested ||
                       !_updates.Reader.Completion.IsCompleted)
                {
                    var update = await _updates.Reader.ReadAsync(_cts.Token);

                    if (_cts.Token.IsCancellationRequested)
                    {
                        break;
                    }

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

        private class OperationStoreObservable
            : IObservable<OperationUpdate>
            , IDisposable
        {
            private readonly object _sync = new();
            private ImmutableList<OperationStoreSession> _sessions =
                ImmutableList<OperationStoreSession>.Empty;

            public void Next(OperationUpdate operationUpdate)
            {
                ImmutableList<OperationStoreSession> sessions = _sessions;

                foreach (var session in sessions)
                {
                    session.Next(operationUpdate);
                }
            }

            public void Complete()
            {
                ImmutableList<OperationStoreSession> sessions = _sessions;

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
                ImmutableList<OperationStoreSession> sessions = _sessions;

                foreach (var session in sessions)
                {
                    session.Complete();
                    session.Dispose();
                }
            }

            private class OperationStoreSession
                : IDisposable
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
}
