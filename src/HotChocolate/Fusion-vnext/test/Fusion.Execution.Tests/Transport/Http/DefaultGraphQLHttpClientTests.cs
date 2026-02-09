using System.Text;
using HotChocolate.Transport;

namespace HotChocolate.Fusion.Transport.Http;

public class DefaultGraphQLHttpClientTests
{
    [Fact]
    public async Task Fetch_Large_Json()
    {
        // arrange
        var context = await GraphQLServerHelper.CreateTestServer();
        using var server = context.Item1;
        await using var app = context.Item2;
        using var client = new DefaultGraphQLHttpClient(server.CreateClient(), disposeInnerClient: true);

        var operationRequest = new OperationRequest("{ items }");
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        using var document = await result.ReadAsResultAsync();

        // assert
        var itemCount = document.Root.GetProperty("data").GetProperty("items").GetArrayLength();
        Assert.Equal(500000, itemCount);

        await app.StopAsync();
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

        var operationRequest = new OperationRequest("{ number }");
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        var document = await result.ReadAsResultAsync();

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

        var operationRequest = new OperationRequest("{ number }");
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        var document = await result.ReadAsResultAsync();

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

        var operationRequest = new OperationRequest("{ number }");
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        var stream = result.ReadAsResultStreamAsync();

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

        var operationRequest = new OperationRequest("{ number }");
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        var stream = result.ReadAsResultStreamAsync();

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
    public async Task ReadAsResultStream_Single_Application_Json_Apollo_Request_Batching_Response()
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
            new OperationRequest("{ number }")
        ]);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        var stream = result.ReadAsResultStreamAsync();

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
    public async Task ReadAsResultStream_Multi_Application_Json_Apollo_Request_Batching_Response()
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
                new OperationRequest("{ number }"),
                new OperationRequest("{ number }")
            ]);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        var stream = result.ReadAsResultStreamAsync();

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
            variables: [
                new Dictionary<string, object?>()
            ]);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        var stream = result.ReadAsResultStreamAsync();

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
            variables: [
              new Dictionary<string, object?>(),
              new Dictionary<string, object?>()
            ]);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        var stream = result.ReadAsResultStreamAsync();

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

        var operationRequest = new OperationRequest("{ number }");
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        var stream = result.ReadAsResultStreamAsync();

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

        var operationRequest = new OperationRequest("{ number }");
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        var stream = result.ReadAsResultStreamAsync();

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
}
