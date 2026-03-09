using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;
using HotChocolate.Transport.Sockets;
using Microsoft.AspNetCore.TestHost;

namespace HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.GraphQLOverWebSocket;

public class SubscriptionTestBase(TestServerFactory serverFactory)
    : ServerTestBase(serverFactory)
{
    protected Uri SubscriptionUri { get; } = new("ws://localhost:5000/graphql");

    protected Task<JsonDocument?> WaitForMessage(
        WebSocket webSocket,
        string type,
        CancellationToken cancellationToken)
        => WaitForMessage(webSocket, type, TimeSpan.FromSeconds(1), cancellationToken);

    protected async Task<JsonDocument?> WaitForMessage(
        WebSocket webSocket,
        string type,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            while (!combinedCts.Token.IsCancellationRequested)
            {
                var message = await webSocket.ReceiveServerMessageAsync(combinedCts.Token);

                if (message is null)
                {
                    await Task.Delay(5, combinedCts.Token);
                    continue;
                }

                var messageType = message.RootElement.GetProperty("type").GetString()!;

                if (type.Equals(messageType))
                {
                    return message;
                }

                throw new InvalidOperationException($"Unexpected message type: {messageType}");
            }
        }
        catch (OperationCanceledException)
        {
            // no message was received in the specified time.
        }

        return null;
    }

    protected Task WaitForConditions(
        Func<bool> condition,
        CancellationToken cancellationToken)
        => WaitForConditions(condition, TimeSpan.FromMilliseconds(500), cancellationToken);

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
        await webSocket.SendConnectionInitAsync(cancellationToken);
        var message = await webSocket.ReceiveServerMessageAsync(cancellationToken);
        Assert.NotNull(message);
        Assert.Equal("connection_ack", message.RootElement.GetProperty("type").GetString());
        return webSocket;
    }

    protected static WebSocketClient CreateWebSocketClient(TestServer testServer)
    {
        var client = testServer.CreateWebSocketClient();
        client.ConfigureRequest = r => r.Headers.SecWebSocketProtocol = WellKnownProtocols.GraphQL_Transport_WS;
        return client;
    }

    protected static async Task TryTest(Func<CancellationToken, Task> action)
    {
        // we will try four times ...
        using var cts = new CancellationTokenSource(Debugger.IsAttached ? 600_000_000 : 15_000);
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
