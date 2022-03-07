using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;
using static HotChocolate.AspNetCore.Subscriptions.ConnectionCloseReason;

namespace HotChocolate.AspNetCore.Subscriptions;

internal sealed class WebSocketSession : ISocketSession
{
    private static readonly GraphQLSocketOptions _defaultOptions = new();
    private bool _disposed;

    private WebSocketSession(
        ISocketConnection connection,
        IProtocolHandler protocol,
        ISocketSessionInterceptor interceptor,
        IRequestExecutor requestExecutor)
    {
        Connection = connection;
        Protocol = protocol;
        Operations = new OperationManager(this, interceptor, requestExecutor);
    }

    public ISocketConnection Connection { get; }

    public IProtocolHandler Protocol { get; }

    public IOperationManager Operations { get; }

    public void Dispose()
    {
        if (!_disposed)
        {
            Operations.Dispose();
            Connection.Dispose();
            _disposed = true;
        }
    }

    public static async Task AcceptAsync(
        HttpContext context,
        IRequestExecutor executor,
        ISocketSessionInterceptor interceptor)
    {
        CancellationToken cancellationToken = context.RequestAborted;
        var connection = new WebSocketConnection(context);
        IProtocolHandler? protocol = await connection.TryAcceptConnection();

        if (protocol is not null)
        {
            var session = new WebSocketSession(connection, protocol, interceptor, executor);
            var options = context.GetGraphQLSocketOptions() ?? _defaultOptions;

            try
            {
                var pipe = new Pipe();
                var pingPong = new PingPongJob(session, options);
                var processor = new MessageProcessor(session, pipe.Reader);
                var receiver = new MessageReceiver(connection, pipe.Writer);

                pingPong.Begin(cancellationToken);
                processor.Begin(cancellationToken);

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
                    await interceptor.OnCloseAsync(session, cancellationToken);
                    await connection.CloseAsync(
                        WebSocketSession_SessionEnded,
                        NormalClosure,
                        CancellationToken.None);
                }
                catch
                {
                    // original exception must not be lost if new exception occurs
                    // during closing session
                }
            }
        }
    }
}
