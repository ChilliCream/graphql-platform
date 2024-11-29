using System.Buffers;
using System.Net.WebSockets;
using HotChocolate.Transport.Sockets.Client.Protocols;
using HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;
using HotChocolate.Utilities;
using static HotChocolate.Transport.Sockets.SocketDefaults;

namespace HotChocolate.Transport.Sockets.Client;

public sealed class SocketClient : ISocket
{
    private static readonly IProtocolHandler[] _protocolHandlers =
    [
        new GraphQLOverWebSocketProtocolHandler(),
    ];

    private readonly CancellationTokenSource _cts = new();
    private readonly CancellationToken _ct;
    private readonly WebSocket _socket;
    private readonly IProtocolHandler _protocol;
    private readonly MessagePipeline _pipeline;
    private readonly SocketClientContext _context;
    private bool _disposed;

    private SocketClient(WebSocket socket, IProtocolHandler protocol)
    {
        _socket = socket;
        _protocol = protocol;
        _context = new SocketClientContext(socket);
        _pipeline = new MessagePipeline(this, new MessageHandler(_context, protocol));
        _pipeline.Completed += (_, _) =>
        {
            if (_context.Socket.CloseStatus is not null)
            {
                _context.Messages.OnError(new SocketClosedException(
                    _context.Socket.CloseStatusDescription ?? "Socket was closed.",
                    _context.Socket.CloseStatus.Value));
            }
            _context.Messages.OnCompleted();
        };
        _ct = _cts.Token;
    }

    public bool IsClosed => _socket.IsClosed();

    public static ValueTask<SocketClient> ConnectAsync(
        WebSocket socket,
        CancellationToken cancellationToken = default)
        => ConnectAsync<object>(socket, null, cancellationToken);

    public static async ValueTask<SocketClient> ConnectAsync<T>(
        WebSocket socket,
        T? payload,
        CancellationToken cancellationToken = default)
    {
        var protocolHandler =
            Array.Find(
                _protocolHandlers,
                t => t.Name.EqualsOrdinal(socket.SubProtocol));

        if (protocolHandler is null)
        {
            throw new NotSupportedException(
                $"The sub-protocol `{socket.SubProtocol}` is not supported.");
        }

        var client = new SocketClient(socket, protocolHandler);
        await client.InitializeAsync(payload, cancellationToken);
        return client;
    }

    private ValueTask InitializeAsync<T>(T payload, CancellationToken cancellationToken)
    {
        BeginRunPipeline();
        return _protocol.InitializeAsync(_context, payload, cancellationToken);
    }

    private void BeginRunPipeline()
        => Task.Factory.StartNew(() => _pipeline.RunAsync(_ct), _ct);

    public ValueTask<SocketResult> ExecuteAsync(
        OperationRequest request,
        CancellationToken cancellationToken = default)
        => _protocol.ExecuteAsync(_context, request, cancellationToken);

    async Task<bool> ISocket.ReadMessageAsync(
        IBufferWriter<byte> writer,
        CancellationToken cancellationToken)
    {
        if (_disposed || _socket.IsClosed())
        {
            return false;
        }

        try
        {
            var read = 0;
            ValueWebSocketReceiveResult socketResult;

            do
            {
                if (_socket.IsClosed())
                {
                    break;
                }

                // get memory from writer
                var memory = writer.GetMemory(BufferSize);

                // read message segment from socket.
                socketResult = await _socket.ReceiveAsync(memory, cancellationToken);

                // advance writer
                writer.Advance(socketResult.Count);
                read += socketResult.Count;
            } while (!socketResult.EndOfMessage);

            return read > 0;
        }
        catch
        {
            // swallow exception, there's nothing we can reasonably do.
            return false;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            _cts.Cancel();
            _cts.Dispose();
            _socket.Dispose();
            _disposed = true;
        }
        return default;
    }

    private sealed class MessageHandler : IMessageHandler
    {
        private readonly SocketClientContext _context;
        private readonly IProtocolHandler _protocolHandler;

        public MessageHandler(SocketClientContext context, IProtocolHandler protocolHandler)
        {
            _context = context;
            _protocolHandler = protocolHandler;
        }

        public async ValueTask OnReceiveAsync(
            ReadOnlySequence<byte> message,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _protocolHandler.OnReceiveAsync(_context, message, cancellationToken);
            }
            finally
            {
                if (_context.Socket.IsClosed())
                {
                    _context.Messages.OnCompleted();
                }
            }
        }
    }
}
