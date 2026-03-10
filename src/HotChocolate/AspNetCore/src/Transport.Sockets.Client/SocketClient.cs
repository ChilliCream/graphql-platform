using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using HotChocolate.Transport.Sockets.Client.Protocols;
using HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;
using HotChocolate.Utilities;
using static HotChocolate.Transport.Sockets.SocketDefaults;

namespace HotChocolate.Transport.Sockets.Client;

public sealed class SocketClient : ISocket
{
    private static readonly IProtocolHandler[] s_protocolHandlers =
    [
        new GraphQLOverWebSocketProtocolHandler()
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
        _pipeline.OnCompleted(
            static context =>
            {
                if (context.Socket.CloseStatus is not null)
                {
                    context.Messages.OnError(
                        new SocketClosedException(
                            context.Socket.CloseStatusDescription ?? "Socket was closed.",
                            context.Socket.CloseStatus.Value));
                }
                context.Messages.OnCompleted();
            },
            _context);
        _ct = _cts.Token;
    }

    public bool IsClosed => _socket.IsClosed();

    public static ValueTask<SocketClient> ConnectAsync(
        WebSocket socket,
        CancellationToken cancellationToken = default)
        => ConnectAsync(socket, default, cancellationToken);

    public static async ValueTask<SocketClient> ConnectAsync(
        WebSocket socket,
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(socket);

        if (payload.ValueKind is not JsonValueKind.Object and not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            throw new ArgumentException(
                "The payload must be an object, null, or undefined.",
                nameof(payload));
        }

        if (socket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException(
                "The WebSocket must be in the open state to connect.");
        }

        var protocolHandler =
            Array.Find(
                s_protocolHandlers,
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

    private ValueTask InitializeAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        BeginRunPipeline();
        return _protocol.InitializeAsync(_context, payload, cancellationToken);
    }

    private void BeginRunPipeline()
        => _pipeline.RunAsync(_ct).FireAndForget();

    public ValueTask<SocketResult> ExecuteAsync(
        OperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _protocol.ExecuteAsync(_context, request, cancellationToken);
    }

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

                // get memory from a writer
                var memory = writer.GetMemory(BufferSize);

                // read a message segment from socket.
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

    private sealed class MessageHandler(
        SocketClientContext context,
        IProtocolHandler protocolHandler)
        : IMessageHandler
    {
        public async ValueTask OnReceiveAsync(
            ReadOnlySequence<byte> message,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await protocolHandler.OnReceiveAsync(context, message, cancellationToken);
            }
            finally
            {
                if (context.Socket.IsClosed())
                {
                    context.Messages.OnCompleted();
                }
            }
        }
    }
}
