using System.Buffers;
using System.Collections.Immutable;
using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Execution.Clients.AliasBatching;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class AliasBatchingHttpSourceSchemaClientTests : FusionTestBase
{
    [Fact]
    public async Task ExecuteAsync_Should_SendAliasedDocumentAndMergedVariables_When_SingleOperationHasTwoRows()
    {
        // arrange
        await using var fixture = await AliasClientTestFixture.CreateAsync();
        var handler = new CapturingHandler(
            """{"data":{"_0":{"name":"Table"},"_1":{"name":"Chair"}}}""");
        await using var client = CreateClient(handler);

        var request = Request(
            """
            query Op($__fusion_2_id: ID!) {
              productById(id: $__fusion_2_id) { name }
            }
            """,
            Row("""{"__fusion_2_id":"P1"}"""),
            Row("""{"__fusion_2_id":"P2"}"""));

        // act
        var results = await ReadAllAsync(
            client.ExecuteAsync(fixture.CreateContext(), request, TestContext.Current.CancellationToken));

        // assert
        NormalizeBody(handler.Body!).MatchInlineSnapshot(
            """
            {
              "query": "query Op($_0__fusion_2_id: ID!, $_1__fusion_2_id: ID!) {\n  _0: productById(id: $_0__fusion_2_id) {\n    name\n  }\n  _1: productById(id: $_1__fusion_2_id) {\n    name\n  }\n}",
              "variables": {
                "_0__fusion_2_id": "P1",
                "_1__fusion_2_id": "P2"
              }
            }
            """);
        Assert.Equal("Table", results[0].Data.GetProperty("productById").GetProperty("name").GetString());
        Assert.Equal("Chair", results[1].Data.GetProperty("productById").GetProperty("name").GetString());
    }

    [Fact]
    public async Task ExecuteBatchAsync_Should_RouteRowsToRequests_When_OperationsHaveMixedCardinality()
    {
        // arrange
        await using var fixture = await AliasClientTestFixture.CreateAsync();
        var handler = new CapturingHandler(
            """
            {"data":{
              "_0_0":{"name":"Table"},
              "_0_1":{"name":"Chair"},
              "_1":{"body":"Great"}
            }}
            """);
        await using var client = CreateClient(handler);

        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) { name }
                }
                """,
                Row("""{"__fusion_2_id":"P1"}"""),
                Row("""{"__fusion_2_id":"P2"}""")),
            Request(
                """
                query Op($__fusion_3_id: ID!) {
                  reviewById(id: $__fusion_3_id) { body }
                }
                """,
                Row("""{"__fusion_3_id":"R1"}""")));

        // act
        var results = new List<SourceSchemaBatchResult>();
        await foreach (var result in client.ExecuteBatchAsync(
            fixture.CreateContext(), requests, TestContext.Current.CancellationToken))
        {
            results.Add(result);
        }

        // assert
        Assert.Equal([0, 0, 1], results.Select(r => r.RequestIndex).ToArray());
        Assert.Equal("Table", results[0].Result.Data.GetProperty("productById").GetProperty("name").GetString());
        Assert.Equal("Great", results[2].Result.Data.GetProperty("reviewById").GetProperty("body").GetString());
    }

    [Fact]
    public async Task ExecuteBatchAsync_Should_Throw_When_RequestIsSubscription()
    {
        // arrange
        await using var fixture = await AliasClientTestFixture.CreateAsync();
        var handler = new CapturingHandler("""{"data":{}}""");
        await using var client = CreateClient(handler);

        var requests = ImmutableArray.Create(
            Request(
                """
                subscription Op { onReview { body } }
                """,
                OperationType.Subscription,
                Row("""{}""")));

        // act
        async Task Act()
        {
            await foreach (var _ in client.ExecuteBatchAsync(
                fixture.CreateContext(),
                requests,
                TestContext.Current.CancellationToken))
            {
            }
        }

        // assert
        await Assert.ThrowsAsync<NotSupportedException>(Act);
    }

    [Fact]
    public async Task ExecuteBatchAsync_Should_Throw_When_RequestRequiresFileUpload()
    {
        // arrange
        await using var fixture = await AliasClientTestFixture.CreateAsync();
        var handler = new CapturingHandler("""{"data":{}}""");
        await using var client = CreateClient(handler);

        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_1_file: Upload!) {
                  upload(file: $__fusion_1_file) { id }
                }
                """,
                OperationType.Query,
                requiresFileUpload: true,
                Row("""{"__fusion_1_file":null}""")));

        // act
        async Task Act()
        {
            await foreach (var _ in client.ExecuteBatchAsync(
                fixture.CreateContext(),
                requests,
                TestContext.Current.CancellationToken))
            {
            }
        }

        // assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(Act);
        Assert.Contains("file uploads", exception.Message);
    }

    [Fact]
    public async Task ExecuteBatchAsync_Should_PropagateTransportError_When_SendThrows()
    {
        // arrange
        await using var fixture = await AliasClientTestFixture.CreateAsync();
        var handler = new ThrowingHandler(new HttpRequestException("connection refused"));
        await using var client = CreateClient(handler);

        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) { name }
                }
                """,
                Row("""{"__fusion_2_id":"P1"}"""),
                Row("""{"__fusion_2_id":"P2"}""")));

        // act
        async Task Act()
        {
            await foreach (var _ in client.ExecuteBatchAsync(
                fixture.CreateContext(),
                requests,
                TestContext.Current.CancellationToken))
            {
            }
        }

        // assert
        await Assert.ThrowsAsync<HttpRequestException>(Act);
    }

    [Fact]
    public async Task ExecuteBatchAsync_Should_DisposeHttpResponse_When_BatchFullyEnumerated()
    {
        // arrange
        await using var fixture = await AliasClientTestFixture.CreateAsync();
        var handler = new TrackingHandler(
            """{"data":{"_0":{"name":"Table"},"_1":{"name":"Chair"}}}""");
        await using var client = CreateClient(handler);

        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) { name }
                }
                """,
                Row("""{"__fusion_2_id":"P1"}"""),
                Row("""{"__fusion_2_id":"P2"}""")));

        // act
        await foreach (var _ in client.ExecuteBatchAsync(
            fixture.CreateContext(), requests, TestContext.Current.CancellationToken))
        {
        }

        // assert
        Assert.True(handler.Response!.IsDisposed);
    }

    [Fact]
    public async Task ExecuteAsync_Should_SendPlainNonAliasedBody_When_SingleRowBypassApplies()
    {
        // arrange
        await using var fixture = await AliasClientTestFixture.CreateAsync();
        var handler = new CapturingHandler("""{"data":{"productById":{"name":"Table"}}}""");
        await using var client = CreateClient(handler);

        var request = Request(
            """
            query Op($__fusion_2_id: ID!) {
              productById(id: $__fusion_2_id) { name }
            }
            """,
            Row("""{"__fusion_2_id":"P1"}"""));

        // act
        var results = await ReadAllAsync(
            client.ExecuteAsync(fixture.CreateContext(), request, TestContext.Current.CancellationToken));

        // assert
        // A single row is sent as a plain GraphQL request: original document, no alias rewriting.
        NormalizeBody(handler.Body!).MatchInlineSnapshot(
            """
            {
              "query": "query Op($__fusion_2_id: ID!) {\n  productById(id: $__fusion_2_id) { name }\n}",
              "variables": {
                "__fusion_2_id": "P1"
              }
            }
            """);
        var result = Assert.Single(results);
        Assert.Equal("Table", result.Data.GetProperty("productById").GetProperty("name").GetString());
    }

    [Fact]
    public async Task ExecuteBatchAsync_Should_InvokeCallbacks_When_ConfiguredOnBatch()
    {
        // arrange
        await using var fixture = await AliasClientTestFixture.CreateAsync();
        string? observedBody = null;
        var resultCount = 0;
        var handler = new CapturingHandler(
            """{"data":{"_0":{"name":"Table"},"_1":{"name":"Chair"}}}""");
        var configuration = new HttpSourceSchemaClientConfiguration(
            "a",
            new Uri("http://localhost:5000/graphql"),
            onBeforeSend: (_, _, message) =>
                observedBody = message.Content!.ReadAsStringAsync().GetAwaiter().GetResult(),
            onSourceSchemaResult: (_, _, _) => Interlocked.Increment(ref resultCount),
            aliasBatching: true);
        await using var client = new AliasBatchingHttpSourceSchemaClient(
            GraphQLHttpClient.Create(new HttpClient(handler)),
            configuration);

        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) { name }
                }
                """,
                Row("""{"__fusion_2_id":"P1"}"""),
                Row("""{"__fusion_2_id":"P2"}""")));

        // act
        var rows = 0;
        await foreach (var _ in client.ExecuteBatchAsync(
            fixture.CreateContext(),
            requests,
            TestContext.Current.CancellationToken))
        {
            rows++;
        }

        // assert
        Assert.Contains("_0__fusion_2_id", observedBody);
        Assert.Equal(2, rows);
        Assert.Equal(2, resultCount);
    }

    private static AliasBatchingHttpSourceSchemaClient CreateClient(HttpMessageHandler handler)
    {
        var configuration = new HttpSourceSchemaClientConfiguration(
            "a",
            new Uri("http://localhost:5000/graphql"),
            aliasBatching: true);

        return new AliasBatchingHttpSourceSchemaClient(
            GraphQLHttpClient.Create(new HttpClient(handler)),
            configuration);
    }

    private static async Task<List<SourceSchemaResult>> ReadAllAsync(
        IAsyncEnumerable<SourceSchemaResult> resultsStream)
    {
        var results = new List<SourceSchemaResult>();

        await foreach (var result in resultsStream.WithCancellation(TestContext.Current.CancellationToken))
        {
            results.Add(result);
        }

        return results;
    }

    private static string NormalizeBody(string body)
    {
        using var document = JsonDocument.Parse(body);
        return JsonSerializer.Serialize(
            document,
            new JsonSerializerOptions { WriteIndented = true });
    }

    private static SourceSchemaClientRequest Request(string operation, params JsonSegment[] rows)
        => Request(operation, OperationType.Query, requiresFileUpload: false, rows);

    private static SourceSchemaClientRequest Request(
        string operation,
        OperationType operationType,
        params JsonSegment[] rows)
        => Request(operation, operationType, requiresFileUpload: false, rows);

    private static SourceSchemaClientRequest Request(
        string operation,
        OperationType operationType,
        bool requiresFileUpload,
        params JsonSegment[] rows)
    {
        var variables = ImmutableArray.CreateBuilder<VariableValues>(rows.Length);

        foreach (var row in rows)
        {
            variables.Add(new VariableValues(CompactPath.Root, row));
        }

        return new SourceSchemaClientRequest
        {
            Node = null!,
            SchemaName = "a",
            OperationType = operationType,
            OperationSourceText = operation,
            OperationHash = operation.ComputeHash(),
            Variables = variables.ToImmutable(),
            RequiresFileUpload = requiresFileUpload
        };
    }

    private static JsonSegment Row(string json)
    {
        var writer = new ChunkedArrayWriter();
        var start = writer.Position;
        var bytes = Encoding.UTF8.GetBytes(json);
        writer.Write(bytes);
        return JsonSegment.Create(writer, start, bytes.Length);
    }

    private sealed class AliasClientTestFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly IRequestExecutor _executor;
        private readonly OperationPlan _operationPlan;
        private readonly List<OperationPlanContext> _rentedContexts = [];
        private readonly List<CancellationTokenSource> _ctsList = [];
        private readonly List<(ObjectPool<PooledRequestContext> Pool, PooledRequestContext Context)>
            _requestContexts = [];

        private AliasClientTestFixture(
            ServiceProvider services,
            IRequestExecutor executor,
            OperationPlan operationPlan)
        {
            _services = services;
            _executor = executor;
            _operationPlan = operationPlan;
        }

        public static async Task<AliasClientTestFixture> CreateAsync()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            var services = serviceCollection
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(
                    ComposeSchemaDocument(
                        """
                        type Query {
                          field: String!
                        }
                        """))
                .Services
                .BuildServiceProvider();

            var executor = await services.GetRequestExecutorAsync();
            var schema = (FusionSchemaDefinition)executor.Schema;
            var operationPlan = PlanOperation(
                schema,
                """
                query {
                  field
                }
                """);

            return new AliasClientTestFixture(services, executor, operationPlan);
        }

        public OperationPlanContext CreateContext()
        {
            var contextPool = _executor.Schema.Services.GetRequiredService<OperationPlanContextPool>();
            var context = contextPool.Rent();
            var cts = new CancellationTokenSource();
            var requestContextPool =
                _executor.Schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
            var requestContext = requestContextPool.Get();
            var request = OperationRequestBuilder.New()
                .SetDocument("{ field }")
                .Build();

            requestContext.Initialize(
                _executor.Schema,
                _executor.Version,
                request,
                requestIndex: 0,
                requestServices: _services,
                requestAborted: CancellationToken.None);

            context.Initialize(requestContext, VariableValueCollection.Empty, _operationPlan, cts, new MemoryArena());

            _ctsList.Add(cts);
            _requestContexts.Add((requestContextPool, requestContext));
            _rentedContexts.Add(context);
            return context;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var context in _rentedContexts)
            {
                await context.DisposeAsync();
            }

            foreach (var (pool, context) in _requestContexts)
            {
                pool.Return(context);
            }

            foreach (var cts in _ctsList)
            {
                cts.Dispose();
            }

            await _services.DisposeAsync();
        }
    }

    private sealed class CapturingHandler(string responseContent) : HttpMessageHandler
    {
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    responseContent,
                    Encoding.UTF8,
                    "application/graphql-response+json")
            };

            return response;
        }
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => throw exception;
    }

    private sealed class TrackingHandler(string responseContent) : HttpMessageHandler
    {
        public TrackingHttpResponseMessage? Response { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new TrackingHttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    responseContent,
                    Encoding.UTF8,
                    "application/graphql-response+json")
            };

            Response = response;
            return Task.FromResult<HttpResponseMessage>(response);
        }
    }

    private sealed class TrackingHttpResponseMessage(HttpStatusCode statusCode)
        : HttpResponseMessage(statusCode)
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
