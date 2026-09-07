using System.Net;
using System.Net.Http.Json;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.TestHost;
using static HotChocolate.AspNetCore.StreamTestSchema;

namespace HotChocolate.AspNetCore;

public class StreamOverHttpTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Theory]
    [InlineData("multipart/mixed", "multipart/mixed", "-----\r\n")]
    [InlineData("text/event-stream", "text/event-stream", "\nevent: complete\n\n")]
    [InlineData("application/jsonl", "application/jsonl", "")]
    public async Task Stream_Should_DeliverPendingIncrementalAndCompleted_When_ItemsCompleteOneByOne(
        string acceptHeader,
        string contentType,
        string termination)
    {
        // arrange
        var source = new StreamSource();
        using var server = CreateStreamServer(ServerFactory, source);
        var client = server.CreateClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // the initial slice and the look-ahead item must be available before execution starts.
        source.Write("a");
        source.Write("b");
        source.Release("a");

        using var request = CreateStreamRequest(acceptHeader);

        // act
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        await using var body = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(body);
        var payloads = new List<string>();

        // every item completes only after the previous payload was read, so the payload
        // boundaries are the delivery boundaries and not an artifact of coalescing.
        payloads.Add(await ReadPayloadAsync(reader, cts.Token));
        source.Release("b");
        payloads.Add(await ReadPayloadAsync(reader, cts.Token));
        source.Write("c");
        source.Release("c");
        payloads.Add(await ReadPayloadAsync(reader, cts.Token));
        source.Complete();
        payloads.Add(await ReadPayloadAsync(reader, cts.Token));

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(termination, await reader.ReadToEndAsync(cts.Token));
        payloads.MatchInlineSnapshots(
        [
            """{"data":{"items":[{"name":"a"}]},"pending":[{"id":"2","path":["items"]}],"hasNext":true}""",
            """{"incremental":[{"id":"2","items":[{"name":"b"}]}],"hasNext":true}""",
            """{"incremental":[{"id":"2","items":[{"name":"c"}]}],"hasNext":true}""",
            """{"completed":[{"id":"2"}],"hasNext":false}"""
        ]);
    }

    [Theory]
    [InlineData("multipart/mixed; incrementalSpec=v0.1")]
    [InlineData("text/event-stream; incrementalSpec=v0.1")]
    [InlineData("application/jsonl; incrementalSpec=v0.1")]
    public async Task Stream_Should_DeliverLegacyItemsAndPath_When_IncrementalSpecIsV0_1(string acceptHeader)
    {
        // arrange
        var source = new StreamSource();
        using var server = CreateStreamServer(ServerFactory, source);
        var client = server.CreateClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        source.Write("a");
        source.Write("b");
        source.Release("a");

        using var request = CreateStreamRequest(acceptHeader);

        // act
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        await using var body = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(body);
        var payloads = new List<string>();

        payloads.Add(await ReadPayloadAsync(reader, cts.Token));
        source.Release("b");
        payloads.Add(await ReadPayloadAsync(reader, cts.Token));
        source.Complete();
        payloads.Add(await ReadPayloadAsync(reader, cts.Token));

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        payloads.MatchInlineSnapshots(
        [
            """{"data":{"items":[{"name":"a"}]},"hasNext":true}""",
            """{"incremental":[{"items":[{"name":"b"}],"path":["items"]}],"hasNext":true}""",
            """{"hasNext":false}"""
        ]);
    }

    [Fact]
    public async Task Stream_Should_RejectOperation_When_AcceptIsGraphQLResponseJsonOnly()
    {
        // arrange
        var source = new StreamSource();
        using var server = CreateStreamServer(ServerFactory, source);
        var client = server.CreateClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        using var request = CreateStreamRequest("application/graphql-response+json");

        // act
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        // assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        (await response.Content.ReadAsStringAsync(cts.Token)).MatchInlineSnapshot(
            """{"errors":[{"message":"The specified operation kind is not allowed."}]}""");
    }

    [Fact]
    public async Task Stream_Should_RejectOperation_When_AcceptIsApplicationJsonOnly()
    {
        // arrange
        var source = new StreamSource();
        using var server = CreateStreamServer(ServerFactory, source);
        var client = server.CreateClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        using var request = CreateStreamRequest("application/json");

        // act
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        // assert
        // the application/json response content-type answers every well-formed request with
        // 200 per graphql-over-http 6.4.1, so the rejection only shows in the response body.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        (await response.Content.ReadAsStringAsync(cts.Token)).MatchInlineSnapshot(
            """{"errors":[{"message":"The specified operation kind is not allowed."}]}""");
    }

    [Fact]
    public async Task Stream_Should_CancelEnumeration_When_ClientDisconnects()
    {
        // arrange
        var source = new StreamSource();
        using var server = CreateStreamServer(ServerFactory, source);
        var client = server.CreateClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        source.Write("a");
        source.Write("b");
        source.Release("a");

        using var request = CreateStreamRequest("application/jsonl");

        // act
        var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        var body = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(body);
        var initialPayload = await ReadPayloadAsync(reader, cts.Token);

        // the second item is never released, so the stream is parked in the enumeration
        // when the client closes the connection.
        response.Dispose();

        // assert
        // the enumerator cleanup only runs once the aborted request cancels the enumeration.
        await source.EnumeratorCleanup.WaitAsync(cts.Token);
        Assert.Equal(
            """{"data":{"items":[{"name":"a"}]},"pending":[{"id":"2","path":["items"]}],"hasNext":true}""",
            initialPayload);
    }

    private static HttpRequestMessage CreateStreamRequest(string acceptHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = JsonContent.Create(new { query = StreamOperation })
        };
        request.Headers.Add("Accept", acceptHeader);
        return request;
    }

    private static async Task<string> ReadPayloadAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith('{'))
            {
                return line;
            }

            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                return line["data: ".Length..];
            }
        }

        throw new InvalidOperationException("The response ended before the next payload was written.");
    }
}
