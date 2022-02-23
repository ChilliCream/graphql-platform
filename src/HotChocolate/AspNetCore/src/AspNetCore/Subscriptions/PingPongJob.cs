using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

namespace HotChocolate.AspNetCore.Subscriptions;

internal sealed class PingPongJob
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);
    private readonly ISocketConnection _connection;
    private readonly TimeSpan _timeout;

    public PingPongJob(ISocketConnection connection)
        : this(connection, _defaultTimeout)
    {
    }

    public PingPongJob(ISocketConnection connection, TimeSpan timeout)
    {
        _connection = connection;
        _timeout = timeout;
    }

    public void Begin(IProtocolHandler protocolHandler, CancellationToken cancellationToken)
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
        try
        {
            while (!_connection.IsClosed && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_timeout, cancellationToken);

                if (!_connection.IsClosed)
                {
                    await _connection.SendAsync(
                        KeepConnectionAliveMessage.Default.Serialize(),
                        cancellationToken);
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
