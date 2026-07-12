using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Transport.Http;

public class DefaultGraphQLHttpClientTests
{
    private const int LargeTestBatchSize = 5_000;

    [Fact]
    public async Task Fetch_Large_Json()
    {
        // arrange
        var context = await GraphQLServerHelper.CreateTestServer();
        using var server = context.Item1;
        await using var app = context.Item2;
        using var client = new DefaultGraphQLHttpClient(server.CreateClient(), disposeInnerClient: true);

        var operationRequest =
            new OperationRequest("{ items }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        using var document = await result.ReadAsResultAsync(CommonTestExtensions.CreateArena(), TestContext.Current.CancellationToken);

        // assert
        var itemCount = document.Root.GetProperty("data").GetProperty("items").GetArrayLength();
        Assert.Equal(500000, itemCount);

        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ReadAsResult_Application_GraphQL_Response_Json_Response()
    {
        // arrange
        var handler = new MockHttpMessageHandler(
            """
            {
              "data": {
                "number": 0
              }
            }
            """,
            "application/graphql-response+json");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var document = await result.ReadAsResultAsync(CommonTestExtensions.CreateArena(), TestContext.Current.CancellationToken);

        // assert
        var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();

        Assert.Equal(0, number);
    }

    [Fact]
    public async Task ReadAsResult_Application_Json_Response()
    {
        // arrange
        var handler = new MockHttpMessageHandler(
            """
            {
              "data": {
                "number": 0
              }
            }
            """,
            "application/json");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var document = await result.ReadAsResultAsync(CommonTestExtensions.CreateArena(), TestContext.Current.CancellationToken);

        // assert
        var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();

        Assert.Equal(0, number);
    }

    [Fact]
    public async Task ReadAsResultStream_Single_Application_GraphQL_Response_Json_Response()
    {
        // arrange
        var handler = new MockHttpMessageHandler(
            """
            {
              "data": {
                "number": 0
              }
            }
            """,
            "application/graphql-response+json");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();

            Assert.Equal(count, number);

            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Single_Application_Json_Response()
    {
        // arrange
        var handler = new MockHttpMessageHandler(
            """
            {
              "data": {
                "number": 0
              }
            }
            """,
            "application/json");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();

            Assert.Equal(count, number);

            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Single_Application_Json_ApolloRequestBatching_Response()
    {
        // arrange
        var handler = new MockHttpMessageHandler(
            """
            [
              {
                "data": {
                  "number": 0
                }
              }
            ]
            """,
            "application/json");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest = new OperationBatchRequest(
        [
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty)
        ]);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();

            Assert.Equal(count, number);

            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Multi_Application_Json_ApolloRequestBatching_Response()
    {
        // arrange
        var handler = new MockHttpMessageHandler(
            """
            [
              {
                "data": {
                  "number": 0
                }
              },
              {
                "data": {
                  "number": 1
                }
              }
            ]
            """,
            "application/json");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest = new OperationBatchRequest(
        [
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty),
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty)
        ]);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();

            Assert.Equal(count, number);

            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Single_Application_Json_Lines_Response()
    {
        // arrange
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("{\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(
            ms,
            "application/jsonl");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest = new VariableBatchRequest(
            "{ number }",
            null,
            null,
            null,
            [VariableValues.Empty],
            JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();

            Assert.Equal(count, number);

            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Multi_Application_Json_Lines_Response()
    {
        // arrange
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("{\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write("{\"data\":{\"number\":1}}");
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(
            ms,
            "application/jsonl");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest = new VariableBatchRequest(
            "{ number }",
            null,
            null,
            null,
            [VariableValues.Empty, VariableValues.Empty],
            JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();

            Assert.Equal(count, number);

            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Single_Text_Event_Stream_Response()
    {
        // arrange
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(
            ms,
            "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();

            Assert.Equal(count, number);

            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Multi_Text_Event_Stream_Response()
    {
        // arrange
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":1}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(
            ms,
            "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();

            Assert.Equal(count, number);

            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Multi_Line_Data()
    {
        // arrange
        // The event payload is split across several data lines that are joined with a single line feed.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":");
        sw.Write('\n');
        sw.Write("data: {\"number\":42}");
        sw.Write('\n');
        sw.Write("data: }");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(42, number);
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Event_Split_Across_Reads()
    {
        // arrange
        // The stream returns one byte per read so every event spans many pipe reads.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":1}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(new DripStream(ms, maxBytesPerRead: 1), "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(count, number);
            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_With_KeepAlive_Comments()
    {
        // arrange
        // Keep-alive comment blocks appear before, between, and after the events.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write(": ping");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write(": ping");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":1}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write(": ping");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(count, number);
            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Payload_Spanning_Several_Segments()
    {
        // arrange
        // A single payload larger than the first geometric arena chunks spans several segments.
        var largeValue = new string('x', 8000);
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write($"data: {{\"data\":{{\"value\":\"{largeValue}\"}}}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ value }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var value = document.Root.GetProperty("data").GetProperty("value").GetString();
            Assert.Equal(largeValue, value);
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Payload_And_Next_Event_In_One_Read()
    {
        // arrange
        // The stream hands back large reads so an event terminator and the next event's beginning land
        // in the same pipe read.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":1}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":2}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(new DripStream(ms, maxBytesPerRead: int.MaxValue), "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(count, number);
            count++;
        }

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Data_Without_Event_Is_Ignored()
    {
        // arrange
        // A data line with no event field defaults to the "message" event type, which is not part of
        // the GraphQL over SSE protocol and is therefore ignored.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var _ in stream)
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Complete_With_Data_Ignores_Data()
    {
        // arrange
        // A complete event terminates the stream and ignores any accompanying data lines.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var _ in stream)
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Unknown_Event_With_Data_Is_Ignored()
    {
        // arrange
        // An event name that is neither "next" nor "complete" is not part of the GraphQL over SSE
        // protocol and is ignored, even when it carries data.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: foo");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var _ in stream)
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Id_Line_Inside_Event_Yields()
    {
        // arrange
        // An id field between the event field and the data field is a known SSE field that is not part
        // of the GraphQL over SSE payload, so it is ignored and the event still yields its document.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("id: 1");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(0, number);
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Comment_Inside_Event_Yields()
    {
        // arrange
        // A comment line between the event field and the data field is a keep-alive that is ignored, so
        // the event still yields its document.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write(": ping");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(0, number);
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Multi_Comment_Block_Is_Ignored()
    {
        // arrange
        // A keep-alive block carries no data, so it produces no event regardless of how many comment
        // lines it contains.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write(": ping");
        sw.Write('\n');
        sw.Write(": ping");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var _ in stream)
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Id_Line_After_Data_Yields()
    {
        // arrange
        // An id field after the data field stops data collection; the event still yields its payload
        // and the trailing line is silently dropped.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write("id: 1");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(0, number);
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Comment_After_Data_Yields()
    {
        // arrange
        // A comment after the data field stops data collection; the event still yields its payload and
        // the trailing comment line is silently dropped.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write(": ping");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(0, number);
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Extra_Blank_Line_Between_Events_Is_Ignored()
    {
        // arrange
        // An extra blank line between events is an empty event that carries no data, so it is ignored
        // and the preceding payload is still yielded.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(0, number);
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Complete_Prefix_Is_Ignored()
    {
        // arrange
        // The event name is matched exactly, so a name that merely begins with "complete" is an unknown
        // event. With no data it produces no event, and the stream completes after the preceding payload.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: completeX");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(0, number);
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Event_Name_Trailing_Whitespace_Is_Ignored()
    {
        // arrange
        // The event name is matched exactly, so trailing whitespace after the name makes it an unknown
        // event that is ignored. The protocol uses a bare "next" without trailing whitespace.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next ");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var _ in stream)
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Complete_With_Id_Line_Terminates()
    {
        // arrange
        // A complete event terminates the stream and ignores the rest of its block, so an id field after
        // the complete event is silently dropped and the stream ends cleanly after the preceding payload.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write("id: 1");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(0, number);
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Lone_Blank_Line_At_End_Completes()
    {
        // arrange
        // A stray blank line at the very end of the stream with nothing pending is ignored; the
        // enumeration completes gracefully after the preceding payload instead of throwing.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(0, number);
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Stray_Blank_Then_Unterminated_Block_At_End_Completes()
    {
        // arrange
        // A stray blank line poisons the block that follows, but the stream ends mid block without a
        // blank-line terminator, so the poisoned block is dropped silently and enumeration completes
        // gracefully with no document.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write('\n');
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var _ in stream)
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Text_Event_Stream_Yield_Then_Stray_Blank_Then_Unterminated_Block_At_End_Yields_Once()
    {
        // arrange
        // The first block yields a document, then a stray blank line poisons the following block which is
        // truncated at EOF without a terminator. The poisoned block is dropped silently, so the stream
        // yields exactly once and completes gracefully.
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":0}}");
        sw.Write('\n');
        sw.Write('\n');
        sw.Write('\n');
        sw.Write("event: next");
        sw.Write('\n');
        sw.Write("data: {\"data\":{\"number\":1}}");
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(0, number);
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReadAsResult_Large_Application_Json_Response()
    {
        // arrange
        var largeJson = GenerateLargeJsonResponse(5000);
        var handler = new MockHttpMessageHandler(largeJson, "application/json");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ items }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var document = await result.ReadAsResultAsync(CommonTestExtensions.CreateArena(), TestContext.Current.CancellationToken);

        // assert
        var itemCount = document.Root.GetProperty("data").GetProperty("items").GetArrayLength();
        Assert.Equal(5000, itemCount);
    }

    [Fact]
    public async Task ReadAsResult_Large_Application_Graphql_Response_Json_Response()
    {
        // arrange
        var largeJson = GenerateLargeJsonResponse(5000);
        var handler = new MockHttpMessageHandler(largeJson, "application/graphql-response+json");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ items }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var document = await result.ReadAsResultAsync(CommonTestExtensions.CreateArena(), TestContext.Current.CancellationToken);

        // assert
        var itemCount = document.Root.GetProperty("data").GetProperty("items").GetArrayLength();
        Assert.Equal(5000, itemCount);
    }

    [Fact]
    public async Task ReadAsResultStream_Large_Application_Json_Lines_Response()
    {
        // arrange
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);

        for (var i = 0; i < 1000; i++)
        {
            sw.Write($"{{\"data\":{{\"number\":{i}}}}}");
            sw.Write('\n');
        }

        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "application/jsonl");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest = new VariableBatchRequest(
            "{ number }",
            null,
            null,
            null,
            [.. Enumerable.Repeat(VariableValues.Empty, 1000)],
            JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(count, number);
            count++;
        }

        Assert.Equal(1000, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Large_Application_Json_Array_Response()
    {
        // arrange
        var largeJson = GenerateLargeJsonArrayResponse(LargeTestBatchSize);
        var handler = new MockHttpMessageHandler(largeJson, "application/json");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest = new OperationBatchRequest(
        [
            ..Enumerable.Range(0, LargeTestBatchSize)
                .Select(_ =>
                    (IOperationRequest)new OperationRequest("{ number }", null, null, null, VariableValues.Empty,
                        JsonSegment.Empty))
        ]);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(count, number);
            count++;
        }

        Assert.Equal(LargeTestBatchSize, count);
    }

    [Fact]
    public async Task ReadAsResultStream_Large_Text_Event_Stream_Response()
    {
        // arrange
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);

        for (var i = 0; i < LargeTestBatchSize; i++)
        {
            sw.Write("event: next");
            sw.Write('\n');
            sw.Write($"data: {{\"data\":{{\"number\":{i}}}}}");
            sw.Write('\n');
            sw.Write('\n');
        }

        sw.Write("event: complete");
        sw.Write('\n');
        sw.Write('\n');
        sw.Flush();
        ms.Position = 0;

        var handler = new MockHttpMessageHandler(ms, "text/event-stream");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest =
            new OperationRequest("{ number }", null, null, null, VariableValues.Empty, JsonSegment.Empty);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ReadAsResultStreamAsync(new FixedMemoryArenaSource(CommonTestExtensions.CreateArena()));

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();
            Assert.Equal(count, number);
            count++;
        }

        Assert.Equal(LargeTestBatchSize, count);
    }

    [Fact]
    public async Task Post_Variables_Do_Not_Escape_Apostrophe_To_Unicode()
    {
        // arrange
        var handler = new CapturingRequestHttpMessageHandler(
            """
            {
              "data": {
                "__typename": "Mutation"
              }
            }
            """,
            "application/json");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        const string description =
            "Now available in three new luminous pastel colors, Bill Curry\u2019s mushroom-shaped Obello Lamp "
            + "continues its journey. Celebrated for his bold use of color and inventive Space Age forms, the "
            + "American designer first created the lamp in 1971. After its reintroduction by GUBI in 2022, in "
            + "the color of frosted glass, the design returns in a trio of luminous pastels, each finished with "
            + "a glossy finish that accentuates its sculptural silhouette. Balancing softness with warmth, this "
            + "palette extends Curry\u2019s legacy with a fresh, contemporary expression. It's still iconic.";

        var variableValues = CreateVariableValues(
            new Dictionary<string, object?> { ["description"] = description });
        var operationRequest = new OperationRequest(
            "mutation($description: String!) { updateDescription(description: $description) }",
            null,
            null,
            null,
            variableValues,
            JsonSegment.Empty);

        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(handler.LastBody);
        Assert.DoesNotContain("\\u0027", handler.LastBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\u2019", handler.LastBody, StringComparison.OrdinalIgnoreCase);

        using var body = JsonDocument.Parse(handler.LastBody);
        var serializedDescription = body.RootElement
            .GetProperty("variables")
            .GetProperty("description")
            .GetString();

        Assert.Equal(description, serializedDescription);
    }

    private static string GenerateLargeJsonResponse(int itemCount)
    {
        var sb = new StringBuilder();
        sb.Append("{\"data\":{\"items\":[");

        for (var i = 0; i < itemCount; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append($"{{\"id\":{i},\"value\":\"item_{i}\"}}");
        }

        sb.Append("]}}");
        return sb.ToString();
    }

    private static string GenerateLargeJsonArrayResponse(int itemCount)
    {
        var sb = new StringBuilder();
        sb.Append('[');

        for (var i = 0; i < itemCount; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append($"{{\"data\":{{\"number\":{i}}}}}");
        }

        sb.Append(']');
        return sb.ToString();
    }

    private static VariableValues CreateVariableValues(Dictionary<string, object?> variables)
    {
        var writer = new ChunkedArrayWriter();
        var startPosition = writer.Position;
        using var jsonWriter = new Utf8JsonWriter(writer,
            new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        JsonSerializer.Serialize(jsonWriter, variables);
        jsonWriter.Flush();
        var length = writer.Position - startPosition;
        return new VariableValues(default, JsonSegment.Create(writer, startPosition, length));
    }

    private sealed class DripStream(Stream inner, int maxBytesPerRead) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => inner.Read(buffer, offset, Math.Min(count, maxBytesPerRead));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => inner.ReadAsync(buffer[..Math.Min(buffer.Length, maxBytesPerRead)], cancellationToken);

        public override void Flush() => inner.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private class MockHttpMessageHandler(Stream responseStream, string contentType) : HttpMessageHandler
    {
        public MockHttpMessageHandler(string responseContent, string contentType)
            : this(new MemoryStream(Encoding.UTF8.GetBytes(responseContent)), contentType)
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StreamContent(responseStream)
            };
            response.Content.Headers.Add("Content-Type", contentType + "; charset=utf-8");
            return Task.FromResult(response);
        }
    }

    private sealed class CapturingRequestHttpMessageHandler(string responseContent, string contentType)
        : HttpMessageHandler
    {
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
            };
            response.Content.Headers.Add("Content-Type", contentType + "; charset=utf-8");
            return response;
        }
    }
}
