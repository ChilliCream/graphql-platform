using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Sockets.Client.Helpers;
using HotChocolate.Transport.Sockets.Client.Protocols;
using HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

namespace HotChocolate.Transport.Sockets.Client;

public class SocketClient : ISocketClient, ISocket
{
    private const int _maxMessageSize = 512;
    private static readonly IProtocolHandler[] _protocolHandlers =
    {
        new GraphQLOverWebSocketProtocolHandler()
    };

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
        _ct = _cts.Token;

    }

    public bool IsClosed => _socket.State is not WebSocketState.Open;

    public static async ValueTask<SocketClient> ConnectAsync<T>(
        WebSocket socket,
        T payload,
        CancellationToken cancellationToken = default)
    {
        IProtocolHandler? protocolHandler =
            _protocolHandlers.FirstOrDefault(t => t.Name.EqualsOrdinal(socket.SubProtocol));

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

    private void BeginRunPipeline() => _pipeline.RunAsync(_ct);

    public IAsyncEnumerable<OperationResult> ExecuteAsync(OperationRequest request)
        => _protocol.ExecuteAsync(_context, request);


#if NET5_0_OR_GREATER
    async Task<bool> ISocket.ReadMessageAsync(
        IBufferWriter<byte> writer,
        CancellationToken cancellationToken)
    {
        if (_disposed || _socket is not { State: WebSocketState.Open })
        {
            return false;
        }

        try
        {
            var read = 0;
            ValueWebSocketReceiveResult socketResult;

            do
            {
                if (_socket.State is not WebSocketState.Open)
                {
                    break;
                }

                // get memory from writer
                Memory<byte> memory = writer.GetMemory(_maxMessageSize);

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
#else
    async Task<bool> ISocket.ReadMessageAsync(
        IBufferWriter<byte> writer,
        CancellationToken cancellationToken)
    {
        if (_disposed || _socket is not { State: WebSocketState.Open })
        {
            return false;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(_maxMessageSize);
        var arraySegment = new ArraySegment<byte>(buffer);

        try
        {
            var read = 0;
            WebSocketReceiveResult socketResult;

            do
            {
                if (_socket.State is not WebSocketState.Open)
                {
                    break;
                }

                // read message segment from socket.
                socketResult = await _socket.ReceiveAsync(arraySegment, cancellationToken);

                // copy message segment to writer.
                Memory<byte> memory = writer.GetMemory(socketResult.Count);
                buffer.AsSpan().Slice(0, socketResult.Count).CopyTo(memory.Span);
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
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
#endif

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

        public ValueTask OnReceiveAsync(
            ReadOnlySequence<byte> message,
            CancellationToken cancellationToken = default)
            => _protocolHandler.OnReceiveAsync(_context, message, cancellationToken);
    }
}
