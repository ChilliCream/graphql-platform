using System.Net.WebSockets;
#if FUSION
using System.Text.Json;
using HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client.Protocols;
#else
namespace HotChocolate.Transport.Sockets.Client.Protocols;
#endif

/// <summary>
/// Represents the context for a WebSocket client.
/// </summary>
internal sealed class SocketClientContext
{
#if FUSION
    /// <summary>
    /// Initializes a WebSocket client context with its connection options.
    /// </summary>
    /// <param name="socket">The WebSocket connection.</param>
    /// <param name="options">The options that configure the client.</param>
    public SocketClientContext(WebSocket socket, SocketClientOptions options)
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="SocketClientContext"/> class with
    /// the specified WebSocket object.
    /// </summary>
    /// <param name="socket">
    /// The <see cref="WebSocket"/> object representing the WebSocket connection.
    /// </param>
    public SocketClientContext(WebSocket socket)
#endif
    {
        Socket = socket;
        Messages = new MessageStream();
#if FUSION
        Options = options;
        Sender = new SocketMessageSender(socket);
#endif
    }

    /// <summary>
    /// Gets the <see cref="WebSocket"/> object representing the WebSocket connection.
    /// </summary>
    public WebSocket Socket { get; }

    /// <summary>
    /// Gets the <see cref="MessageStream"/> object representing the message stream
    /// for the WebSocket connection.
    /// </summary>
    public MessageStream Messages { get; }

#if FUSION
    public SocketClientOptions Options { get; }

    public SocketMessageSender Sender { get; }
#endif
}

#if FUSION
internal sealed class SocketMessageSender(WebSocket socket)
{
    private readonly SemaphoreSlim _sendGate = new(1, 1);

    public async ValueTask SendConnectionInitMessageAsync(
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await socket.SendConnectionInitMessage(payload, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendGate.Release();
        }
    }

    public async ValueTask SendSubscribeMessageAsync(
        string id,
        IOperationRequest request,
        CancellationToken cancellationToken)
    {
        await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await socket.SendSubscribeMessageAsync(id, request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendGate.Release();
        }
    }

    public async ValueTask SendSubscribeMessageAsync(
        string id,
        OperationBatchRequest request,
        CancellationToken cancellationToken)
    {
        await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await socket.SendSubscribeMessageAsync(id, request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendGate.Release();
        }
    }

    public async ValueTask SendPongMessageAsync(CancellationToken cancellationToken)
    {
        await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await socket.SendPongMessageAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendGate.Release();
        }
    }

    public async Task TrySendCompleteMessageAsync(string id)
    {
        await _sendGate.WaitAsync().ConfigureAwait(false);

        try
        {
            using var cts = new CancellationTokenSource(2000);

            if (socket.IsOpen())
            {
                await socket.SendCompleteMessageAsync(id, cts.Token).ConfigureAwait(false);
            }
        }
        catch
        {
            try
            {
                socket.Abort();
            }
            catch
            {
                // ignore
            }
        }
        finally
        {
            _sendGate.Release();
        }
    }

    public async ValueTask CloseAsync(
        WebSocketCloseStatus closeStatus,
        string? statusDescription,
        CancellationToken cancellationToken)
    {
        await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (socket.IsOpen())
            {
                await socket.CloseAsync(closeStatus, statusDescription, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _sendGate.Release();
        }
    }
}
#endif
