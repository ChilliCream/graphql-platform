using System.Net.WebSockets;

namespace HotChocolate.AspNetCore.Subscriptions;

internal sealed class PingPongJob(ISocketSession session, GraphQLSocketOptions options)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var connection = session.Connection;
        var protocolHandler = session.Protocol;

        try
        {
            // first, we will wait for a connection to be established
            await Task.Delay(options.ConnectionInitializationTimeout, cancellationToken);

            // if after the timeout no connection initialization was sent by the client, we will
            // close the connection.
            if (!connection.IsConnected)
            {
                await session.Protocol.OnConnectionInitTimeoutAsync(session, cancellationToken);
                return;
            }

            // if a keep alive interval is configured we will start sending connection
            // keep alive messages to the client.
            if (options.KeepAliveInterval is not null)
            {
                var interval = options.KeepAliveInterval.Value;

                while (!connection.IsClosed && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(interval, cancellationToken);

                    if (!connection.IsClosed)
                    {
                        await protocolHandler.SendKeepAliveMessageAsync(session, cancellationToken);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // the message processing was canceled.
        }
        catch (WebSocketException)
        {
            // we will just stop receiving
        }
    }
}
