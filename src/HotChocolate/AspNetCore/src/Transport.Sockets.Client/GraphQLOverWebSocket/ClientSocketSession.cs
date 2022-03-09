using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Reactive.Linq;
using static System.StringComparison;
using static System.Threading.CancellationToken;
using static HotChocolate.Utilities.Transport.Sockets.Protocols.GraphQLOverWebSocket.Messages;

namespace HotChocolate.Utilities.Transport.Sockets.Protocols.GraphQLOverWebSocket;

internal class ClientSocketSession : IClientSocketSession
{
    private readonly CancellationTokenSource _cts = new();
    private readonly MessageSender _sender;
    private readonly MessageReceiver _receiver;
    private readonly MessageProcessor _processor;
    private bool _connected;
    private bool _disposed;

    public ClientSocketSession(WebSocket socket)
    {
        Socket = socket ?? throw new ArgumentNullException(nameof(socket));

        var pipe = new Pipe();

        _sender = new MessageSender(socket);
        _receiver = new MessageReceiver(socket, pipe.Writer);
        _processor = new MessageProcessor(pipe.Reader);

        _receiver.Begin(_cts.Token);
        _processor.Begin(_cts.Token);
    }

    public WebSocket Socket { get; }

    public async ValueTask InitializeAsync<T>(
        T payload,
        CancellationToken cancellationToken = default)
    {
        if (_connected)
        {
            throw new InvalidOperationException("Initialize cannot be invoked twice.");
        }

        var promise = new TaskCompletionSource<bool>();

        _processor
            .FirstOrDefaultAsync(t => t.Type.EqualsOrdinal(ConnectionAccept))
            .Finally(() => promise.TrySetCanceled())
            .Subscribe(_ => promise.TrySetResult(true), cancellationToken);

        await _sender.SendConnectionInitMessage(payload, cancellationToken).ConfigureAwait(false);
        await promise.Task.ConfigureAwait(false);
    }

    public IAsyncEnumerable<object> ExecuteAsync(OperationRequest request)
        => new RequestExecution(_sender, _processor, request);

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            _cts.Cancel();
            _cts.Dispose();

            await Socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "User closed connection.",
                    None)
                .ConfigureAwait(false);

            _disposed = true;
        }
    }

    private sealed class RequestExecution : IAsyncEnumerable<OperationResult>
    {
        private readonly string _operationSessionId = Guid.NewGuid().ToString("N");
        private readonly MessageSender _sender;
        private readonly IObservable<OperationMessage> _messageStream;
        private readonly OperationRequest _request;
        private bool _started;
        private bool _completed;

        public RequestExecution(
            MessageSender sender,
            IObservable<OperationMessage> messageStream,
            OperationRequest request)
        {
            _sender = sender ??
                throw new ArgumentNullException(nameof(sender));
            _messageStream = messageStream ??
                throw new ArgumentNullException(nameof(messageStream));
            _request = request;
        }

        public async IAsyncEnumerator<OperationResult> GetAsyncEnumerator(
            CancellationToken ct = default)
        {
            if (_started)
            {
                throw new InvalidOperationException(
                    "The result stream can only be enumerated once.");
            }
            _started = true;

            await using IAsyncEnumerator<DataMessage> results = _messageStream
                .OfType<DataMessage>()
                .Where(t => t.Id.Equals(_operationSessionId, OrdinalIgnoreCase))
                .ToAsyncEnumerator(ct);

            await _sender.SendSubscribeMessageAsync(_operationSessionId, _request, ct);

            // make sure to send the complete message if the operation is cancelled
            ct.Register(BeginSendCompleteMessage);

            var moveNext = true;

            while(moveNext)
            {
                try
                {
                    moveNext = await results.MoveNextAsync().ConfigureAwait(false);
                }
                finally
                {
                    // if we have an error the stream is already completed and we are done.
                    _completed = true;
                }

                if (moveNext)
                {
                    yield return results.Current.Payload;
                }
            }

            // the stream was completed and we do not send a complete message.
            _completed = true;
        }

        private void BeginSendCompleteMessage()
        {
            if (_completed)
            {
                return;
            }

            Task.Factory.StartNew(
                () => _sender.SendCompleteMessageAsync(_operationSessionId, None),
                None,
                TaskCreationOptions.None,
                TaskScheduler.Default);
        }
    }
}

