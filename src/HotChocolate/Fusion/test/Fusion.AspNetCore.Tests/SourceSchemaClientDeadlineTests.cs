using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Fusion;

public class SourceSchemaClientDeadlineTests : FusionTestBase
{
    private const string UserSchema =
        """
        type Query {
          user(id: ID!): User @lookup
        }

        type User @key(fields: "id") {
          id: ID!
          name: String!
        }
        """;

    private const string ReviewSchema =
        """
        type Query {
          userById(id: ID!): User @lookup
        }

        type User @key(fields: "id") {
          id: ID!
          reviews: [Review!]!
        }

        type Review {
          title: String!
        }
        """;

    [Theory]
    [InlineData(30)]
    [InlineData(100)]
    public async Task Execute_Should_BypassHttpClient_When_ExecutionTimeoutIsAtMost100Seconds(int seconds)
    {
        // arrange
        var recorder = new SendPathRecorder();
        using var server = CreateSourceSchema("A", UserSchema);
        using var gateway = await CreateGatewayAsync(
            [("A", server)],
            recorder,
            executionTimeout: TimeSpan.FromSeconds(seconds));
        using var client = gateway.CreateClient();

        // act
        await PostAsync(client, """{ user(id: "1") { name } }""");

        // assert
        Assert.Equal(["A: HttpMessageInvoker"], recorder.TakeAll());
    }

    [Fact]
    public async Task Execute_Should_UseHttpClient_When_ExecutionTimeoutExceeds100Seconds()
    {
        // arrange
        var recorder = new SendPathRecorder();
        using var server = CreateSourceSchema("A", UserSchema);
        using var gateway = await CreateGatewayAsync(
            [("A", server)],
            recorder,
            executionTimeout: TimeSpan.FromSeconds(101));
        using var client = gateway.CreateClient();

        // act
        await PostAsync(client, """{ user(id: "1") { name } }""");

        // assert
        Assert.Equal(["A: HttpClient"], recorder.TakeAll());
    }

    [Fact]
    public async Task Execute_Should_UseHttpClient_When_HttpClientHasConfigureCallback()
    {
        // arrange
        var recorder = new SendPathRecorder();
        using var server = CreateSourceSchema("A", UserSchema);
        using var gateway = await CreateGatewayAsync(
            [("A", server)],
            recorder,
            configureHttpClient: true);
        using var client = gateway.CreateClient();

        // act
        await PostAsync(client, """{ user(id: "1") { name } }""");

        // assert
        Assert.Equal(["A: HttpClient"], recorder.TakeAll());
    }

    [Fact]
    public async Task Execute_Should_UseHttpClient_When_FieldsAreDeferred()
    {
        // arrange
        var recorder = new SendPathRecorder();
        using var serverA = CreateSourceSchema("A", UserSchema);
        using var serverB = CreateSourceSchema("B", ReviewSchema);
        using var gateway = await CreateGatewayAsync([("A", serverA), ("B", serverB)], recorder);
        using var client = gateway.CreateClient();

        // act
        await PostAsync(client, """{ user(id: "1") { name reviews { title } } }""");
        var withoutDefer = recorder.TakeAll();

        await PostAsync(
            client,
            """{ user(id: "1") { name ... @defer { reviews { title } } } }""",
            accept: "text/event-stream");
        var withDefer = recorder.TakeAll();

        // assert
        new[] { string.Join(", ", withoutDefer), string.Join(", ", withDefer) }.MatchInlineSnapshots(
        [
            "A: HttpMessageInvoker, B: HttpMessageInvoker",
            "A: HttpClient, B: HttpClient"
        ]);
    }

    [Fact]
    public async Task Subscribe_Should_UseHttpClient_When_SharedClientIsUsed()
    {
        // arrange
        var recorder = new SendPathRecorder();
        using var streamServer = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<BookStream.Query>()
                .AddSubscriptionType<BookStream.Subscription>());
        using var titleServer = CreateSourceSchema(
            "B",
            b => b.AddQueryType<BookTitle.Query>());
        using var gateway = await CreateGatewayAsync([("A", streamServer), ("B", titleServer)], recorder);
        using var client = gateway.CreateClient();

        // act
        await PostAsync(client, "subscription { onBookCreated { id title } }", accept: "text/event-stream");

        // assert
        Assert.Equal(["A: HttpClient", "B: HttpClient"], recorder.TakeAll());
    }

    [Fact]
    public async Task Execute_Should_ReturnSameResponse_When_RequestBypassesHttpClient()
    {
        // arrange
        var recorder = new SendPathRecorder();
        using var serverA = CreateSourceSchema("A", UserSchema);
        using var serverB = CreateSourceSchema("B", ReviewSchema);
        using var bypassingGateway = await CreateGatewayAsync(
            [("A", serverA), ("B", serverB)],
            recorder);
        using var httpClientGateway = await CreateGatewayAsync(
            [("A", serverA), ("B", serverB)],
            recorder,
            executionTimeout: TimeSpan.FromSeconds(101));
        var results = new List<string>();

        // act
        foreach (var gateway in new[] { bypassingGateway, httpClientGateway })
        {
            using var client = gateway.CreateClient();
            var body = await PostAsync(client, """{ user(id: "1") { name reviews { title } } }""");
            results.Add(
                body
                + Environment.NewLine
                + FormatInteractions(gateway)
                + Environment.NewLine
                + string.Join(", ", recorder.TakeAll()));
        }

        // assert
        results.MatchInlineSnapshots(
        [
            """
            {"data":{"user":{"name":"User: VXNlcjoyMDkyMTcyNjc2","reviews":[{"title":"Review: UmV2aWV3OjE4NDE5NzQ4Mjk="},{"title":"Review: UmV2aWV3OjMxMjUwNzc5NDI="},{"title":"Review: UmV2aWV3OjI2OTUyMzAxMDE="}]}}}
            A: OK application/graphql-response+json; charset=utf-8
            B: OK application/graphql-response+json; charset=utf-8
            A: HttpMessageInvoker, B: HttpMessageInvoker
            """,
            """
            {"data":{"user":{"name":"User: VXNlcjoyMDkyMTcyNjc2","reviews":[{"title":"Review: UmV2aWV3OjE4NDE5NzQ4Mjk="},{"title":"Review: UmV2aWV3OjMxMjUwNzc5NDI="},{"title":"Review: UmV2aWV3OjI2OTUyMzAxMDE="}]}}}
            A: OK application/graphql-response+json; charset=utf-8
            B: OK application/graphql-response+json; charset=utf-8
            A: HttpClient, B: HttpClient
            """
        ]);
    }

    private async Task<Gateway> CreateGatewayAsync(
        (string SchemaName, TestServer Server)[] servers,
        SendPathRecorder recorder,
        TimeSpan? executionTimeout = null,
        bool configureHttpClient = false)
        => await CreateCompositeSchemaAsync(
            servers,
            configureServices: services =>
            {
                services.RemoveAll<IHttpClientFactory>();

                foreach (var (name, server) in servers)
                {
                    var builder = services
                        .AddHttpClient(name)
                        .ConfigurePrimaryHttpMessageHandler(() => server.CreateHandler())
                        .AddHttpMessageHandler(() => new SendPathHandler(name, recorder));

                    if (configureHttpClient)
                    {
                        builder.ConfigureHttpClient(_ => { });
                    }
                }
            },
            configureGatewayBuilder: builder =>
            {
                if (executionTimeout is { } timeout)
                {
                    builder.ModifyRequestOptions(o => o.ExecutionTimeout = timeout);
                }
            },
            includeOperationPlan: false);

    private static async Task<string> PostAsync(HttpClient client, string query, string? accept = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");

        if (accept is not null)
        {
            request.Headers.Accept.ParseAdd(accept);
        }

        request.Content = new StringContent(
            JsonSerializer.Serialize(new { query }),
            Encoding.UTF8,
            "application/json");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static string FormatInteractions(Gateway gateway)
        => string.Join(
            Environment.NewLine,
            gateway.Interactions
                .OrderBy(t => t.Key, StringComparer.Ordinal)
                .SelectMany(t => t.Value.Values.Select(i => $"{t.Key}: {i.StatusCode} {i.ContentType}")));

    private sealed class SendPathRecorder
    {
        private readonly ConcurrentQueue<string> _paths = [];

        public void Record(string schemaName)
            => _paths.Enqueue($"{schemaName}: {(IsSentByHttpClient() ? "HttpClient" : "HttpMessageInvoker")}");

        public string[] TakeAll()
        {
            var paths = _paths.ToArray();
            _paths.Clear();
            return paths;
        }

        // HttpClient calls the handler pipeline synchronously from its send frame,
        // so an HttpClient frame is on the stack only when HttpClient sends the request.
        private static bool IsSentByHttpClient()
        {
            foreach (var frame in new StackTrace().GetFrames())
            {
                for (var type = frame.GetMethod()?.DeclaringType; type is not null; type = type.DeclaringType)
                {
                    if (type == typeof(HttpClient))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    private sealed class SendPathHandler(string schemaName, SendPathRecorder recorder) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            recorder.Record(schemaName);
            return base.SendAsync(request, cancellationToken);
        }
    }

    public static class BookStream
    {
        [EntityKey("id")]
        public record Book(int Id);

        public class Query
        {
            public string Foo() => "Foo";
        }

        public class Subscription
        {
            public async IAsyncEnumerable<Book> OnBookCreatedStream()
            {
                await Task.Yield();
                yield return new Book(1);
            }

            [Subscribe(With = nameof(OnBookCreatedStream))]
            public Book OnBookCreated([EventMessage] Book book) => book;
        }
    }

    public static class BookTitle
    {
        public record Book(int Id, string Title);

        public class Query
        {
            [Internal, Lookup]
            public Book? GetBookById(int id) => new(id, $"Title {id}");
        }
    }
}
