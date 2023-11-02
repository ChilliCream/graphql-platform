using System.Diagnostics;
using System.Net.WebSockets;
using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;
using Microsoft.AspNetCore.TestHost;

namespace HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.Apollo;

public class SubscriptionTestBase : ServerTestBase
{
    public SubscriptionTestBase(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    protected Uri SubscriptionUri { get; } = new("ws://localhost:5000/graphql");

    protected Task<IReadOnlyDictionary<string, object?>?> WaitForMessage(
        WebSocket webSocket,
        string type,
        CancellationToken cancellationToken)
        => WaitForMessage(webSocket, type, TimeSpan.FromMilliseconds(500), cancellationToken);

    protected async Task<IReadOnlyDictionary<string, object?>?> WaitForMessage(
        WebSocket webSocket,
        string type,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token);

        try
        {
            while (!combinedCts.Token.IsCancellationRequested)
            {
                await Task.Delay(50, combinedCts.Token);

                var message = await webSocket.ReceiveServerMessageAsync(combinedCts.Token);

                if (message != null && type.Equals(message[MessageProperties.Type]))
                {
                    return message;
                }

                if (message?[MessageProperties.Type]?.Equals("ka") is false)
                {
                    throw new InvalidOperationException(
                        $"Unexpected message type: {message[MessageProperties.Type]}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // no massage was received in the specified time.
        }

        return null;
    }

    protected Task WaitForConditions(
        Func<bool> condition,
        CancellationToken cancellationToken)
        => WaitForConditions(condition, TimeSpan.FromSeconds(5), cancellationToken);

    protected async Task WaitForConditions(
        Func<bool> condition,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();

        try
        {
            while (timer.Elapsed <= timeout)
            {
                await Task.Delay(50, cancellationToken);

                if (condition())
                {
                    return;
                }
            }
        }
        finally
        {
            timer.Stop();
        }
    }

    protected async Task<WebSocket> ConnectToServerAsync(
        WebSocketClient client,
        CancellationToken cancellationToken)
    {
        var webSocket = await client.ConnectAsync(SubscriptionUri, cancellationToken);
        await webSocket.SendConnectionInitializeAsync(cancellationToken);
        var message = await webSocket.ReceiveServerMessageAsync(cancellationToken);
        Assert.NotNull(message);
        Assert.Equal("connection_ack", message?[MessageProperties.Type]);
        return webSocket;
    }

    protected static WebSocketClient CreateWebSocketClient(TestServer testServer)
    {
        var client = testServer.CreateWebSocketClient();
        client.ConfigureRequest = r => r.Headers.SecWebSocketProtocol = "graphql-ws";
        return client;
    }

    protected static async Task TryTest(Func<CancellationToken, Task> action)
    {
        // we will try four times ....
        using var cts = new CancellationTokenSource(Debugger.IsAttached ? 600_000_000 : 60_000);
        var ct = cts.Token;
        var count = 0;
        var wait = 50;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            if (count < 3)
            {
                try
                {
                    await action(ct).ConfigureAwait(false);
                    break;
                }
                catch
                {
                    // try again
                }
            }
            else
            {
                await action(ct).ConfigureAwait(false);
                break;
            }

            await Task.Delay(wait, ct).ConfigureAwait(false);
            wait *= 2;
            count++;
        }
    }
}
