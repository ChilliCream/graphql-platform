using System.Net.WebSockets;

namespace HotChocolate.AspNetCore.Subscriptions;

internal sealed class PingPongJob
{
    private readonly ISocketSession _session;
    private readonly GraphQLSocketOptions _options;

    public PingPongJob(ISocketSession session, GraphQLSocketOptions options)
    {
        _session = session;
        _options = options;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var connection = _session.Connection;
        var protocolHandler = _session.Protocol;

        try
        {
            // first we will wait for a connection to be established
            await Task.Delay(_options.ConnectionInitializationTimeout, cancellationToken);

            // if after the timeout no connection initialization was send by the client we will
            // close the connection.
            if (!connection.ContextData.ContainsKey(ConnectionContextKeys.Connected))
            {
                await _session.Protocol.OnConnectionInitTimeoutAsync(_session, cancellationToken);
                return;
            }

            // if a keep alive interval is configured we will start sending connection
            // keep alive messages to the client.
            if (_options.KeepAliveInterval is not null)
            {
                var interval = _options.KeepAliveInterval.Value;

                while (!connection.IsClosed && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(interval, cancellationToken);

                    if (!connection.IsClosed)
                    {
                        await protocolHandler.SendKeepAliveMessageAsync(_session, cancellationToken);
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
