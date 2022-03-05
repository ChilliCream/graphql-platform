using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions.Apollo;

public class SubscriptionTestBase : ServerTestBase
{
    public SubscriptionTestBase(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    protected Uri SubscriptionUri { get; } = new("ws://localhost:5000/graphql");

    protected async Task<IReadOnlyDictionary<string, object>> WaitForMessage(
        WebSocket webSocket,
        string type,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token);

        while (!combinedCts.Token.IsCancellationRequested)
        {
            await Task.Delay(50, combinedCts.Token);

            var message = await webSocket.ReceiveServerMessageAsync(combinedCts.Token);

            if (message != null && type.Equals(message["type"]))
            {
                return message;
            }

            if (message?["type"].Equals("ka") == false)
            {
                throw new InvalidOperationException(
                    $"Unexpected message type: {message["type"]}");
            }
        }

        return null;
    }

    protected async Task WaitForConditions(Func<bool> condition, TimeSpan timeout)
    {
        var timer = Stopwatch.StartNew();

        try
        {
            while (timer.Elapsed <= timeout)
            {
                await Task.Delay(50);

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
        Assert.Equal("connection_ack", message["type"]);
        return webSocket;
    }

    protected static WebSocketClient CreateWebSocketClient(TestServer testServer)
    {
        WebSocketClient client = testServer.CreateWebSocketClient();
        client.ConfigureRequest = r => r.Headers.Add("Sec-WebSocket-Protocol", "graphql-ws");
        return client;
    }

    protected static async Task TryTest(Func<CancellationToken, Task> action)
    {
        // we will try four times ....
        using var cts = new CancellationTokenSource(Debugger.IsAttached ? 600_000_000 : 60_000);
        CancellationToken ct = cts.Token;
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
