using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using static HotChocolate.Utilities.Transport.Sockets.Protocols.GraphQLOverWebSocket.Utf8MessageProperties;

namespace HotChocolate.Utilities.Transport.Sockets.Protocols.GraphQLOverWebSocket;

internal sealed class MessageProcessor
    : MessageProcessorBase
    , IObservable<OperationMessage>
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly List<Subscription> _subscriptions = new();

    public MessageProcessor(PipeReader reader) : base(reader)
    {

    }

    protected override ValueTask ProcessMessageAsync(
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken)
    {
        var document = JsonDocument.Parse(message);
        JsonElement root = document.RootElement;

        if (root.TryGetProperty(TypeProp, out JsonElement typeProp))
        {
            if (typeProp.ValueEquals(Utf8Messages.Ping))
            {

            }
            else if (typeProp.ValueEquals(Utf8Messages.Pong))
            {

            }
            else if (typeProp.ValueEquals(Utf8Messages.Next))
            {
                JsonElement payload = root.GetProperty(PayloadProp);
                payload.TryGetProperty(DataProp, out JsonElement dataProp);
                payload.TryGetProperty(ErrorsProp, out JsonElement errorsProp);
                payload.TryGetProperty(ExtensionsProp, out JsonElement extensionsProp);

                var operationResult = new OperationResult(
                    document,
                    dataProp,
                    errorsProp,
                    extensionsProp);

                var dataMessage = new DataMessage(
                    root.GetProperty(IdProp).GetString()!,
                    Messages.Next,
                    operationResult);

                OnNext(dataMessage);
            }
            else if (typeProp.ValueEquals(Utf8Messages.Error))
            {

            }
            else if (typeProp.ValueEquals(Utf8Messages.ConnectionAccept))
            {
                OnNext(ConnectionAcceptMessage.Default);
            }
        }

        return default;
    }

    protected override ValueTask CompletedAsync()
    {


        return default;
    }

    public IDisposable Subscribe(IObserver<OperationMessage> observer)
    {
        var subscription = new Subscription(observer, this);

        _lock.EnterWriteLock();

        try
        {
            _subscriptions.Add(subscription);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return subscription;
    }

    private void Unsubscribe(Subscription subscription)
    {
        _lock.EnterWriteLock();

        try
        {
            _subscriptions.Remove(subscription);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void OnNext(OperationMessage message)
    {
        _lock.EnterReadLock();

        try
        {
            foreach (Subscription subscription in _subscriptions)
            {
                subscription.Observer.OnNext(message);
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void OnError(Exception error)
    {
        _lock.EnterWriteLock();

        try
        {
            foreach (Subscription subscription in _subscriptions)
            {
                subscription.Observer.OnError(error);
                subscription.Observer.OnCompleted();
            }
            _subscriptions.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void OnComplete()
    {
        _lock.EnterWriteLock();

        try
        {
            foreach (Subscription subscription in _subscriptions)
            {
                subscription.Observer.OnCompleted();
            }
            _subscriptions.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private class Subscription : IDisposable
    {
        private readonly MessageProcessor _processor;
        private bool _disposed;

        public Subscription(
            IObserver<OperationMessage> observer,
            MessageProcessor processor)
        {
            Observer = observer;
            _processor = processor;
        }

        public IObserver<OperationMessage> Observer { get; }

        public void Dispose()
        {
            if (!_disposed)
            {
                _processor.Unsubscribe(this);
                _disposed = true;
            }
        }
    }
}
