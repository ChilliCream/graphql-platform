using System.Net.WebSockets;
using HotChocolate.AspNetCore.Subscriptions.Protocols;

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

    public void Begin(CancellationToken cancellationToken)
        => Task.Factory.StartNew(
            () => KeepConnectionAliveAsync(cancellationToken),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private async Task KeepConnectionAliveAsync(CancellationToken ct)
    {
        ISocketConnection connection = _session.Connection;
        IProtocolHandler protocolHandler = _session.Protocol;

        try
        {
            // first we will wait for a connection to be established
            await Task.Delay(_options.ConnectionInitializationTimeout, ct);

            // if after the timeout no connection initialization was send by the client we will
            // close the connection.
            if (!connection.ContextData.ContainsKey(ConnectionContextKeys.Connected))
            {
                await _session.Protocol.OnConnectionInitTimeoutAsync(_session, ct);
                return;
            }

            // if a keep alive interval is configured we will start sending connection
            // keep alive messages to the client.
            if (_options.KeepAliveInterval is not null)
            {
                TimeSpan interval = _options.KeepAliveInterval.Value;

                while (!connection.IsClosed && !ct.IsCancellationRequested)
                {
                    await Task.Delay(interval, ct);

                    if (!connection.IsClosed)
                    {
                        await protocolHandler.SendKeepAliveMessageAsync(_session, ct);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // the message processing was canceled.
        }
        catch (WebSocketException)
        {
            // we will just stop receiving
        }
    }
}
