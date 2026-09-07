using System.Net.WebSockets;
using System.Text.Json;
using HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.GraphQLOverWebSocket;
using static HotChocolate.AspNetCore.StreamTestSchema;

namespace HotChocolate.AspNetCore.Subscriptions.GraphQLOverWebSocket;

public class StreamOverWebSocketTests(TestServerFactory serverFactory) : SubscriptionTestBase(serverFactory)
{
    private static readonly TimeSpan s_messageTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Stream_Should_SendNextFramePerPayloadThenComplete_When_OperationUsesStream()
    {
        // arrange
        var source = new StreamSource();
        using var testServer = CreateStreamServer(ServerFactory, source);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = CreateWebSocketClient(testServer);
        using var webSocket = await client.ConnectAsync(SubscriptionUri, cts.Token);

        // the connection is only initialized, there is no capability negotiation for
        // incremental delivery.
        await webSocket.SendConnectionInitAsync(cts.Token);
        using var connectionAccept =
            await ReceiveMessageAsync(webSocket, Messages.ConnectionAccept, cts.Token);

        // the initial slice and the look-ahead item must be available before execution starts.
        source.Write("a");
        source.Write("b");
        source.Release("a");

        // act
        await webSocket.SendSubscribeAsync("abc", new SubscribePayload(StreamOperation), cts.Token);

        var payloads = new List<string>();

        // every item completes only after the previous frame was received, so the frame
        // boundaries are the delivery boundaries and not an artifact of coalescing.
        payloads.Add(await ReceiveNextPayloadAsync(webSocket, cts.Token));
        source.Release("b");
        payloads.Add(await ReceiveNextPayloadAsync(webSocket, cts.Token));
        source.Complete();
        payloads.Add(await ReceiveNextPayloadAsync(webSocket, cts.Token));

        using var complete = await ReceiveMessageAsync(webSocket, Messages.Complete, cts.Token);

        // assert
        Assert.Equal("abc", complete.RootElement.GetProperty(MessageProperties.Id).GetString());
        payloads.MatchInlineSnapshots(
        [
            """{"data":{"items":[{"name":"a"}]},"pending":[{"id":"2","path":["items"]}],"hasNext":true}""",
            """{"incremental":[{"id":"2","items":[{"name":"b"}]}],"hasNext":true}""",
            """{"completed":[{"id":"2"}],"hasNext":false}"""
        ]);
    }

    private async Task<string> ReceiveNextPayloadAsync(
        WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        using var message = await ReceiveMessageAsync(webSocket, Messages.Next, cancellationToken);
        return message.RootElement.GetProperty(MessageProperties.Payload).GetRawText();
    }

    private async Task<JsonDocument> ReceiveMessageAsync(
        WebSocket webSocket,
        string type,
        CancellationToken cancellationToken)
        => await WaitForMessage(webSocket, type, s_messageTimeout, cancellationToken)
            ?? throw new InvalidOperationException($"The server did not send a {type} message.");
}
