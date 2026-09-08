using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
#if FUSION
using HotChocolate.Buffers;
using HotChocolate.Fusion.Transport.Sockets.Client.Protocols;
using HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;
using static HotChocolate.Fusion.Transport.Sockets.SocketDefaults;
#else
using HotChocolate.Transport.Sockets.Client.Protocols;
using HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;
using HotChocolate.Utilities;
#endif
#if !FUSION
using static HotChocolate.Transport.Sockets.SocketDefaults;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client;
#else
namespace HotChocolate.Transport.Sockets.Client;
#endif

public sealed class SocketClient : ISocket
{
    // The close code the client reports when the connection is lost without a close frame
    // (for example a TCP reset, a server crash, or a proxy idle-kill). It mirrors the
    // 1006 "abnormal closure" code that the WebSocket protocol reserves for this condition.
    private const WebSocketCloseStatus AbnormalClosure = (WebSocketCloseStatus)1006;

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

#if FUSION
    private SocketClient(
        WebSocket socket,
        IProtocolHandler protocol,
        SocketClientOptions options)
#else
    private SocketClient(WebSocket socket, IProtocolHandler protocol)
#endif
    {
        _socket = socket;
        _protocol = protocol;
#if FUSION
        _context = new SocketClientContext(socket, options);
#else
        _context = new SocketClientContext(socket);
#endif
        _pipeline = new MessagePipeline(this, new MessageHandler(_context, protocol));
        _ct = _cts.Token;
        var ct = _ct;
        _pipeline.OnCompleted(
            context =>
            {
                if (context.Socket.CloseStatus is not null)
                {
                    // the server ended the connection with a close frame, so we surface the
                    // close status the server reported.
                    context.Messages.OnError(
                        new SocketClosedException(
                            context.Socket.CloseStatusDescription ?? "Socket was closed.",
                            context.Socket.CloseStatus.Value));
                }
                else if (!ct.IsCancellationRequested)
                {
                    // the connection was lost without a close frame and the client did not
                    // initiate the teardown, so the completion is not clean.
                    context.Messages.OnError(
                        new SocketClosedException(
                            "Connection closed abnormally (no close frame received).",
                            AbnormalClosure));
                }

                context.Messages.OnCompleted();
            },
            _context);
    }

    public bool IsClosed => _socket.IsClosed();

#if FUSION
    public static ValueTask<SocketClient> ConnectAsync(
        WebSocket socket,
        SocketClientOptions options,
        CancellationToken cancellationToken = default)
        => ConnectAsync(socket, options, default, cancellationToken);

    public static async ValueTask<SocketClient> ConnectAsync(
        WebSocket socket,
        SocketClientOptions options,
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentNullException.ThrowIfNull(options);
#else
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
#endif

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
#if FUSION
                t => string.Equals(t.Name, socket.SubProtocol, StringComparison.Ordinal));
#else
                t => t.Name.EqualsOrdinal(socket.SubProtocol));
#endif

        if (protocolHandler is null)
        {
            throw new NotSupportedException(
                $"The sub-protocol `{socket.SubProtocol}` is not supported.");
        }

#if FUSION
        var client = new SocketClient(socket, protocolHandler, options);
#else
        var client = new SocketClient(socket, protocolHandler);
#endif

        try
        {
            await client.InitializeAsync(payload, cancellationToken);
        }
        catch
        {
            // the handshake faulted (cancellation, a missing ack, or a send failure), so the
            // fire-and-forget receive pipeline is still running. Dispose the client to cancel it
            // before the failure propagates, otherwise the client is unreachable but not torn down.
            await client.DisposeAsync();
            throw;
        }

        return client;
    }

    private ValueTask InitializeAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        BeginRunPipeline();
        return _protocol.InitializeAsync(_context, payload, cancellationToken);
    }

    private void BeginRunPipeline()
#if FUSION
        => _ = _pipeline.RunAsync(_ct);
#else
        => _pipeline.RunAsync(_ct).FireAndForget();
#endif

#if FUSION
    /// <summary>
    /// Closes the socket after all previously queued operation messages have been sent.
    /// </summary>
    /// <param name="closeStatus">The close status to send to the server.</param>
    /// <param name="statusDescription">The optional description to send with the close status.</param>
    /// <param name="cancellationToken">The cancellation token for the close handshake.</param>
    public async ValueTask CloseAsync(
        WebSocketCloseStatus closeStatus,
        string? statusDescription,
        CancellationToken cancellationToken)
    {
        try
        {
            await _context.Sender.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    public ValueTask<SocketResult> ExecuteAsync(
        IOperationRequest request,
        IMemoryArenaSource arenaSource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(arenaSource);

        return _protocol.ExecuteAsync(
            _context,
            request,
            arenaSource,
            deferPayloadParsing: false,
            cancellationToken);
    }

    public ValueTask<SocketResult> SubscribeAsync(
        IOperationRequest request,
        IMemoryArenaSource arenaSource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(arenaSource);

        return _protocol.ExecuteAsync(
            _context,
            request,
            arenaSource,
            deferPayloadParsing: true,
            cancellationToken);
    }
#else
    public ValueTask<SocketResult> ExecuteAsync(
        IOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _protocol.ExecuteAsync(_context, request, cancellationToken);
    }
#endif

#if FUSION
    public ValueTask<SocketResult> ExecuteBatchAsync(
        OperationBatchRequest request,
        IMemoryArenaSource arenaSource,
        CancellationToken cancellationToken = default)
    {
        if (request.Requests.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                "The batch request must contain at least one operation.",
                nameof(request));
        }

        ArgumentNullException.ThrowIfNull(arenaSource);

        return _protocol.ExecuteBatchAsync(
            _context,
            request,
            arenaSource,
            cancellationToken);
    }
#else
    public ValueTask<SocketResult> ExecuteBatchAsync(
        OperationBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Requests.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                "The batch request must contain at least one operation.",
                nameof(request));
        }

        return _protocol.ExecuteBatchAsync(_context, request, cancellationToken);
    }
#endif

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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
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
        public ValueTask OnReceiveAsync(
            ReadOnlySequence<byte> message,
            CancellationToken cancellationToken = default)
            => protocolHandler.OnReceiveAsync(context, message, cancellationToken);
    }
}
