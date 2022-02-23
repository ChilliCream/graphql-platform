using System.IO.Pipelines;
using HotChocolate.AspNetCore.Properties;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions;

internal sealed class WebSocketSession : ISocketSession
{
    private readonly Pipe _pipe = new();
    private readonly ISocketConnection _connection;
    private readonly bool _disposeConnection;
    private readonly ISocketSessionInterceptor _sessionInterceptor;
    private bool _disposed;

    private WebSocketSession(
        ISocketSessionInterceptor sessionInterceptor,
        ISocketConnection connection,
        bool disposeConnection)
    {
        _connection = connection;
        _disposeConnection = disposeConnection;
        _sessionInterceptor = sessionInterceptor;
    }

    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        IProtocolHandler? protocolHandler = await _connection.TryAcceptConnection();

        if (protocolHandler is not null)
        {
            try
            {
                var pingPong = new PingPongJob(_connection);
                var processor = new MessageProcessor(_connection, _pipe.Reader);
                var receiver = new MessageReceiver(_connection, _pipe.Writer);

                pingPong.Begin(protocolHandler, cancellationToken);
                processor.Begin(protocolHandler, cancellationToken);
                await receiver.ReceiveAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // OperationCanceledException are caught and will not
                // bubble further. We will just close the current subscription
                // context.
            }
            finally
            {
                try
                {
                    await _connection.CloseAsync(
                        AspNetCoreResources.WebSocketSession_SessionEnded,
                        ConnectionCloseReason.NormalClosure,
                        CancellationToken.None);
                    await _sessionInterceptor.OnCloseAsync(_connection, cancellationToken);
                }
                catch
                {
                    // original exception must not be lost if new exception occurs
                    // during closing session
                }
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_disposeConnection)
            {
                _connection.Dispose();
            }

            _disposed = true;
        }
    }

    public static WebSocketSession New(
        HttpContext httpContext,
        ISocketSessionInterceptor socketSessionInterceptor)
    {
        if (httpContext is null)
        {
            throw new ArgumentNullException(nameof(httpContext));
        }

        if (socketSessionInterceptor is null)
        {
            throw new ArgumentNullException(nameof(socketSessionInterceptor));
        }

        var connection = WebSocketConnection.New(httpContext);

        return new WebSocketSession(socketSessionInterceptor, connection, true);
    }
}
