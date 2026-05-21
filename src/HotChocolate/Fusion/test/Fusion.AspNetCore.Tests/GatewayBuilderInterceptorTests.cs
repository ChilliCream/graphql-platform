using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Transport.Http;
using HotChocolate.Transport.Sockets;
using HotChocolate.Transport.Sockets.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OperationResult = HotChocolate.Execution.OperationResult;
using TransportOperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.Fusion;

public class GatewayBuilderInterceptorTests : FusionTestBase
{
    private const string SimpleSchema =
        """
        type Query {
          field: String
        }
        """;

    private const string ExtensionKey = "captured";

    [Fact]
    public async Task AddHttpRequestInterceptor_Generic_Should_Be_Invoked()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b
                .AddHttpRequestInterceptor<ExtensionsHttpRequestInterceptor>()
                .UseRequest(EchoExtensionsMiddleware, key: "EchoExtensions"));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync("{ field }", new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        Assert.Equal("from-http-interceptor", response.Extensions.GetProperty(ExtensionKey).GetString());
    }

    [Fact]
    public async Task AddHttpRequestInterceptor_Factory_Should_Be_Invoked()
    {
        // arrange
        var factoryInvoked = false;
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b
                .AddHttpRequestInterceptor(
                    _ =>
                    {
                        factoryInvoked = true;
                        return new ExtensionsHttpRequestInterceptor();
                    })
                .UseRequest(EchoExtensionsMiddleware, key: "EchoExtensions"));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync("{ field }", new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        Assert.Equal("from-http-interceptor", response.Extensions.GetProperty(ExtensionKey).GetString());
        Assert.True(factoryInvoked);
    }

    [Fact]
    public async Task AddSocketSessionInterceptor_Generic_Should_Be_Invoked()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b
                .AddSocketSessionInterceptor<ExtensionsSocketSessionInterceptor>()
                .UseRequest(EchoExtensionsMiddleware, key: "EchoExtensions"));

        // act
        var extensions = await SubscribeOverWebSocketAsync(gateway);

        // assert
        Assert.Equal("from-socket-interceptor", extensions.GetProperty(ExtensionKey).GetString());
    }

    [Fact]
    public async Task AddSocketSessionInterceptor_Factory_Should_Be_Invoked()
    {
        // arrange
        var factoryInvoked = false;
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b
                .AddSocketSessionInterceptor(
                    _ =>
                    {
                        factoryInvoked = true;
                        return new ExtensionsSocketSessionInterceptor();
                    })
                .UseRequest(EchoExtensionsMiddleware, key: "EchoExtensions"));

        // act
        var extensions = await SubscribeOverWebSocketAsync(gateway);

        // assert
        Assert.Equal("from-socket-interceptor", extensions.GetProperty(ExtensionKey).GetString());
        Assert.True(factoryInvoked);
    }

    [Fact]
    public async Task AddHttpResponseFormatter_Indented_Should_Indent_Response_Body()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b
                .AddHttpRequestInterceptor<DefaultHttpRequestInterceptor>()
                .AddHttpResponseFormatter(indented: true));

        // act
        var body = await PostQueryAndReadBodyAsync(gateway, "{ field }");

        // assert
        Assert.Equal(
            """
            {
              "data": {
                "field": "Query"
              }
            }
            """,
            body);
    }

    [Fact]
    public async Task AddHttpResponseFormatter_Generic_Should_Use_Custom_Formatter()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.AddHttpResponseFormatter<TeapotHttpResponseFormatter>());

        var client = gateway.CreateClient();

        // act
        var statusCode = await PostQueryAndReadStatusAsync(client, "{ field }");

        // assert
        Assert.Equal((HttpStatusCode)418, statusCode);
    }

    [Fact]
    public async Task AddHttpResponseFormatter_Factory_Should_Use_Custom_Formatter()
    {
        // arrange
        var factoryInvoked = false;
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.AddHttpResponseFormatter(
                _ =>
                {
                    factoryInvoked = true;
                    return new TeapotHttpResponseFormatter();
                }));

        var client = gateway.CreateClient();

        // act
        var statusCode = await PostQueryAndReadStatusAsync(client, "{ field }");

        // assert
        Assert.Equal((HttpStatusCode)418, statusCode);
        Assert.True(factoryInvoked);
    }

    private static RequestMiddleware EchoExtensionsMiddleware =>
        (_, _) => context =>
        {
            if (context.Request.Extensions is { } extensions
                && extensions.Document.RootElement.TryGetProperty(ExtensionKey, out var value)
                && value.GetString() is { } stringValue)
            {
                context.Result = new OperationResult(
                    ImmutableOrderedDictionary<string, object?>.Empty.Add(ExtensionKey, stringValue));
            }

            return default;
        };

    private static async Task<string> PostQueryAndReadBodyAsync(Gateway gateway, string query)
    {
        using var http = gateway.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5000/graphql")
        {
            Content = JsonContent.Create(new { query })
        };
        request.Headers.Add("Accept", "application/graphql-response+json");
        using var response = await http.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<HttpStatusCode> PostQueryAndReadStatusAsync(HttpClient client, string query)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5000/graphql")
        {
            Content = JsonContent.Create(new { query })
        };
        request.Headers.Add("Accept", "application/graphql-response+json");
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static async Task<JsonElement> SubscribeOverWebSocketAsync(Gateway gateway)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var ct = cts.Token;

        var webSocketClient = gateway.CreateWebSocketClient();
        webSocketClient.ConfigureRequest = r => r.Headers.SecWebSocketProtocol = WellKnownProtocols.GraphQL_Transport_WS;

        using var webSocket = await webSocketClient.ConnectAsync(
            new Uri("ws://localhost:5000/graphql"),
            ct);

        await using var client = await SocketClient.ConnectAsync(webSocket, ct);
        using var result = await client.ExecuteAsync(new TransportOperationRequest("{ field }"), ct);

        await foreach (var operationResult in result.ReadResultsAsync().WithCancellation(ct))
        {
            using (operationResult)
            {
                return operationResult.Extensions.Clone();
            }
        }

        throw new InvalidOperationException("No result received over the WebSocket.");
    }

    private sealed class ExtensionsHttpRequestInterceptor : DefaultHttpRequestInterceptor
    {
        public override async ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);

            requestBuilder.SetExtensions(
                JsonSerializer.SerializeToDocument(
                    new Dictionary<string, string> { [ExtensionKey] = "from-http-interceptor" }));
        }
    }

    private sealed class ExtensionsSocketSessionInterceptor : DefaultSocketSessionInterceptor
    {
        public override ValueTask OnRequestAsync(
            ISocketSession session,
            string operationSessionId,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken = default)
        {
            requestBuilder.SetExtensions(
                JsonSerializer.SerializeToDocument(
                    new Dictionary<string, string> { [ExtensionKey] = "from-socket-interceptor" }));

            return default;
        }
    }

    private sealed class TeapotHttpResponseFormatter : DefaultHttpResponseFormatter
    {
        protected override HttpStatusCode OnDetermineStatusCode(
            OperationResult result,
            FormatInfo format,
            HttpStatusCode? proposedStatusCode)
            => (HttpStatusCode)418;
    }
}
