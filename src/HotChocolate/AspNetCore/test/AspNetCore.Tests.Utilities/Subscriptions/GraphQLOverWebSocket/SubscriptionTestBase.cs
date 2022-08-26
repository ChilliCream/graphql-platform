using System.Diagnostics;
using System.Net.WebSockets;
using HotChocolate.Transport.Sockets;
using Microsoft.AspNetCore.TestHost;
using Xunit.Abstractions;

namespace HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.GraphQLOverWebSocket;

public class SubscriptionTestBase : ServerTestBase
{
    public SubscriptionTestBase(TestServerFactory serverFactory, ITestOutputHelper testOutputHelper)
        : base(serverFactory, testOutputHelper)
    {
    }

    protected Uri SubscriptionUri { get; } = new("ws://localhost:5000/graphql");

    protected Task<IReadOnlyDictionary<string, object?>?> WaitForMessage(
        WebSocket webSocket,
        string type,
        CancellationToken cancellationToken)
        => WaitForMessage(webSocket, type, TimeSpan.FromSeconds(1), cancellationToken);

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
                var message = await webSocket.ReceiveServerMessageAsync(combinedCts.Token);

                if(message is null)
                {
                    await Task.Delay(5, combinedCts.Token);
                    continue;
                }

                if (type.Equals(message["type"]))
                {
                    return message;
                }

                throw new InvalidOperationException($"Unexpected message type: {message["type"]}");
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
        Assert.Equal("connection_ack", message?["type"]);
        return webSocket;
    }

    protected static WebSocketClient CreateWebSocketClient(TestServer testServer)
    {
        var client = testServer.CreateWebSocketClient();
        client.ConfigureRequest = r => r.Headers.Add(
            "Sec-WebSocket-Protocol",
            WellKnownProtocols.GraphQL_Transport_WS);
        return client;
    }

    protected async Task TryTest(Func<CancellationToken, Task> action)
    {
        // we will try four times ....
        using var cts = new CancellationTokenSource(Debugger.IsAttached ? 600_000_000 : 15_000);
        var ct = cts.Token;
        var count = 1;
        var wait = 50;

        var exceptions = new List<Exception>();
        while (count <= 4)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                TestOutputHelper?.WriteLine($"Executing Test Attempt (#{count})...");

                await action(ct)
                    .ConfigureAwait(false);

                TestOutputHelper?.WriteLine($"Completed Test Attempt (#{count}) successfully...");

                break;
            }
            catch(Exception ex)
            {
                TestOutputHelper?.WriteLine($"Completed Test Attempt (#{count}) Unsuccessfully...");
                TestOutputHelper?.WriteLine(ex.Message);

                exceptions.Add(ex);
            }

            await Task.Delay(wait, ct).ConfigureAwait(false);
            wait *= 2;
            count++;
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException(exceptions);
        }
    }
}
