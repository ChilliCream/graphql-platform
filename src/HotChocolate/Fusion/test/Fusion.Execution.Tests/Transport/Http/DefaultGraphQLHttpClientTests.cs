using System.Text;
using System.Text.Json;
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

        var operationRequest = new OperationRequest(
            "mutation($description: String!) { updateDescription(description: $description) }",
            variables: new Dictionary<string, object?>
            {
                ["description"] = description
            });

        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await client.SendAsync(request);

        // assert
        Assert.NotNull(handler.LastBody);
        Assert.DoesNotContain("\\u0027", handler.LastBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\u2019", handler.LastBody, StringComparison.OrdinalIgnoreCase);

        using var body = JsonDocument.Parse(handler.LastBody!);
        var serializedDescription = body.RootElement
            .GetProperty("variables")
            .GetProperty("description")
            .GetString();

        Assert.Equal(description, serializedDescription);
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
