using System.Net.WebSockets;
using HotChocolate.AspNetCore.Subscriptions.Protocols;

namespace HotChocolate.AspNetCore.Subscriptions;

internal sealed class PingPongJob
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);
    private readonly ISocketSession _session;
    private readonly TimeSpan _timeout;

    public PingPongJob(ISocketSession session)
        : this(session, _defaultTimeout)
    {
    }

    public PingPongJob(ISocketSession session, TimeSpan timeout)
    {
        _session = session;
        _timeout = timeout;
    }

    public void Begin(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(
            () => KeepConnectionAliveAsync(cancellationToken),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private async Task KeepConnectionAliveAsync(
        CancellationToken cancellationToken)
    {
        ISocketConnection connection = _session.Connection;
        IProtocolHandler protocolHandler = _session.Protocol;

        try
        {
            while (!connection.IsClosed && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_timeout, cancellationToken);

                if (!connection.IsClosed)
                {
                    await protocolHandler.SendKeepAliveMessageAsync(_session, cancellationToken);
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
