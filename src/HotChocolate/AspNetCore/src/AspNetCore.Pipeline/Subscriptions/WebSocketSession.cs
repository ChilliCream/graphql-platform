using System.Buffers;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Transport.Sockets;
using Microsoft.AspNetCore.Http;
using static HotChocolate.AspNetCore.Properties.AspNetCorePipelineResources;
using static HotChocolate.AspNetCore.Subscriptions.ConnectionCloseReason;

namespace HotChocolate.AspNetCore.Subscriptions;

internal sealed class WebSocketSession : ISocketSession
{
    private static readonly GraphQLSocketOptions s_defaultOptions = new();
    private bool _disposed;

    private WebSocketSession(
        ISocketConnection connection,
        IProtocolHandler protocol,
        ExecutorSession executorSession)
    {
        Connection = connection;
        Protocol = protocol;
        Operations = new OperationManager(this, executorSession);
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
        ExecutorSession executorSession)
    {
        using var connection = new WebSocketConnection(context, executorSession);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted,
            connection.ApplicationStopping);
        var ct = cts.Token;
        var protocol = await connection.TryAcceptConnection();

        if (protocol is not null)
        {
            using var session = new WebSocketSession(connection, protocol, executorSession);
            var options = context.GetGraphQLSocketOptions() ?? s_defaultOptions;

            try
            {
                var pingPong = new PingPongJob(session, options);
                var pipeline = new MessagePipeline(connection, new ProtocolMessageHandler(session));
                pipeline.OnCompleted(static cts => cts.Cancel(), cts);
                await Task.WhenAll(pingPong.RunAsync(ct), pipeline.RunAsync(ct));
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
                    await executorSession.SocketSessionInterceptor.OnCloseAsync(session,
                        connection.HttpContext.RequestAborted);

                    if (!connection.IsClosed)
                    {
                        // ensure that the connection is closed at the end.
                        await connection.CloseAsync(
                            WebSocketSession_SessionEnded,
                            NormalClosure,
                            CancellationToken.None);
                    }
                }
                catch
                {
                    // the original exception must not be lost if new exception occurs
                    // during closing session
                }
            }
        }
    }

    private sealed class ProtocolMessageHandler(ISocketSession session) : IMessageHandler
    {
        private readonly IProtocolHandler _protocol = session.Protocol;

        public ValueTask OnReceiveAsync(
            ReadOnlySequence<byte> message,
            CancellationToken cancellationToken = default)
            => _protocol.OnReceiveAsync(session, message, cancellationToken);
    }
}
