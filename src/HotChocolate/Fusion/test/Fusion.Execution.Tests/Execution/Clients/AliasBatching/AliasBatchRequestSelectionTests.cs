using System.Collections.Immutable;
using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Fusion.Execution.Clients.AliasBatching.AliasBatchTestData;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

public sealed class AliasBatchRequestSelectionTests : FusionTestBase
{
    private const string Lookup = "query($__fusion_1_id: ID!){fieldById(id: $__fusion_1_id){field}}";

    private const string TwoRootFieldLookup =
        "query($__fusion_1_id: ID!){fieldById(id: $__fusion_1_id){field} field}";

    [Theory]
    [InlineData(SourceSchemaClientCapabilities.AliasBatching, true)]
    [InlineData(
        SourceSchemaClientCapabilities.AliasBatching | SourceSchemaClientCapabilities.VariableBatching,
        true)]
    [InlineData(
        SourceSchemaClientCapabilities.AliasBatching | SourceSchemaClientCapabilities.RequestBatching,
        true)]
    [InlineData(
        SourceSchemaClientCapabilities.AliasBatching | SourceSchemaClientCapabilities.All,
        false)]
    [InlineData(SourceSchemaClientCapabilities.All, false)]
    [InlineData(SourceSchemaClientCapabilities.None, false)]
    public async Task ExecuteAsync_Should_SelectTheBatchedBody_When_CapabilitiesAllowIt(
        SourceSchemaClientCapabilities capabilities,
        bool expectsAliasBatching)
    {
        // arrange
        await using var fixture = await Fixture.CreateAsync(capabilities);

        // act
        var body = await fixture.SendAsync(fixture.CreateLookupRequest());

        // assert
        Assert.Equal(expectsAliasBatching, body.StartsWith("""{"query":"query Batch_""", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_Should_KeepTheClassicBody_When_RequestUploadsFiles()
    {
        // arrange
        await using var fixture = await Fixture.CreateAsync(SourceSchemaClientCapabilities.AliasBatching);
        var request = fixture.CreateLookupRequest() with { RequiresFileUpload = true };

        // act
        var body = await fixture.SendAsync(request);

        // assert
        Assert.StartsWith("""[{"query":""", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_Should_KeepTheClassicBody_When_OperationIsAMutation()
    {
        // arrange
        await using var fixture = await Fixture.CreateAsync(SourceSchemaClientCapabilities.AliasBatching);
        var request = fixture.CreateLookupRequest() with { OperationType = OperationType.Mutation };

        // act
        var body = await fixture.SendAsync(request);

        // assert
        Assert.StartsWith("""[{"query":""", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_Should_IncludeOnError_When_TheCapabilityProvidesAMode()
    {
        // arrange
        await using var fixture = await Fixture.CreateAsync(
            SourceSchemaClientCapabilities.AliasBatching,
            ErrorHandlingMode.Propagate);

        // act
        var body = await fixture.SendAsync(fixture.CreateLookupRequest());

        // assert
        Assert.EndsWith("""},"onError":"PROPAGATE"}""", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_Should_KeepTheClassicBody_When_OperationSelectsTwoRootFields()
    {
        // arrange
        await using var fixture = await Fixture.CreateAsync(SourceSchemaClientCapabilities.AliasBatching);
        // The plan resolved a lookup type for this operation, but its document reads from two root
        // selections, so it cannot be rewritten into an aliased copy of itself.
        var request = fixture.CreateLookupRequest() with { Document = fixture.TwoRootFieldDocument };

        // act
        var body = await fixture.SendAsync(request);

        // assert
        Assert.StartsWith("""[{"query":""", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteBatchAsync_Should_SendTheRequestOnItsOwn_When_ItSelectsTwoRootFields()
    {
        // arrange
        await using var fixture = await Fixture.CreateAsync(SourceSchemaClientCapabilities.AliasBatching);
        var batched = fixture.CreateLookupRequest();
        var isolated = fixture.CreateLookupRequest() with
        {
            Document = fixture.TwoRootFieldDocument,
            Variables = [fixture.CreateVariableValues("3")]
        };

        // act
        var requestIndices = await fixture.SendBatchAsync([batched, isolated]);

        // assert
        // The two variable sets of the batched request are answered by the batched response, and
        // the request that cannot join it is answered by a round-trip of its own.
        Assert.Equal([0, 0, 1], requestIndices);
    }

    [Fact]
    public async Task ExecuteAsync_Should_PrefixTheVariable_When_TheClientDeclaresItButTheNodeDoesNotForwardIt()
    {
        // arrange
        await using var fixture = await Fixture.CreateAsync(SourceSchemaClientCapabilities.AliasBatching);
        // The client request declares a variable that carries the name of the per-item variable of
        // the operation, but the node does not forward it, so its value still differs per item.
        var clientVariables = fixture.CreateClientVariables("__fusion_1_id");

        // act
        var body = await fixture.SendAsync(fixture.CreateLookupRequest(), clientVariables);

        // assert
        ReadVariables(body).MatchInlineSnapshot(
            """{"_0___fusion_1_id":"1","_1___fusion_1_id":"2"}""");
    }

    [Fact]
    public async Task ExecuteBatchAsync_Should_PrefixEveryValue_When_AnotherRequestCarriesTheNamePerItem()
    {
        // arrange
        await using var fixture = await Fixture.CreateAsync(SourceSchemaClientCapabilities.AliasBatching);
        var perItem = fixture.CreateLookupRequest();
        // The second request forwards a client variable that carries the name the first request
        // carries a value per item for, so the name cannot be declared once for the whole batch.
        var forwarding = fixture.CreateLookupRequest() with
        {
            Variables = [fixture.CreateVariableValues("B")],
            ForwardedVariables = ["__fusion_1_id"]
        };

        // act
        await fixture.SendBatchAsync(
            [perItem, forwarding],
            fixture.CreateClientVariables("__fusion_1_id"));

        // assert
        ReadVariables(fixture.Body).MatchInlineSnapshot(
            """{"_0___fusion_1_id":"1","_1___fusion_1_id":"2","_2___fusion_1_id":"B"}""");
    }

    [Fact]
    public async Task ExecuteAsync_Should_FreeTheDocument_When_TheEnumerationIsAbandoned()
    {
        // arrange
        await using var fixture = await Fixture.CreateAsync(SourceSchemaClientCapabilities.AliasBatching);

        // act
        // The caller stops after the first item, so the items it never received are freed with the
        // enumeration while the item it received keeps the response document alive.
        var first = await fixture.ReadFirstAsync(fixture.CreateLookupRequest());
        var root = first.AliasedRoot;
        var readWhileHeld = root.GetProperty("field").GetString();
        first.Dispose();

        // assert
        Assert.Equal("A", readWhileHeld);
        Assert.Throws<ObjectDisposedException>(() => root.GetProperty("field").GetString());
    }

    private static string ReadVariables(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("variables").GetRawText();
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly IRequestExecutor _executor;
        private readonly OperationPlan _operationPlan;
        private readonly ChunkedArrayWriter _memory = new();
        private readonly Utf8OperationDocument _document = ParseLookup(Lookup);
        private readonly Utf8OperationDocument _twoRootFieldDocument = ParseLookup(TwoRootFieldLookup);
        private readonly CapturingHandler _handler = new();
        private readonly HttpSourceSchemaClient _client;
        private readonly List<OperationPlanContext> _contexts = [];
        private readonly List<CancellationTokenSource> _ctsList = [];
        private readonly List<(ObjectPool<PooledRequestContext> Pool, PooledRequestContext Context)>
            _requestContexts = [];

        private Fixture(
            ServiceProvider services,
            IRequestExecutor executor,
            OperationPlan operationPlan,
            SourceSchemaClientCapabilities capabilities,
            ErrorHandlingMode? onError)
        {
            _services = services;
            _executor = executor;
            _operationPlan = operationPlan;
            _client = new HttpSourceSchemaClient(
                new DefaultGraphQLHttpClient(new HttpClient(_handler), disposeInnerClient: true),
                new HttpSourceSchemaClientConfiguration(
                    "A",
                    new Uri("http://localhost:5000/graphql"),
                    capabilities: capabilities,
                    onError: onError));
        }

        public static async Task<Fixture> CreateAsync(
            SourceSchemaClientCapabilities capabilities,
            ErrorHandlingMode? onError = null)
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
            var operationPlan = PlanOperation(schema, "{ field }");

            return new Fixture(services, executor, operationPlan, capabilities, onError);
        }

        /// <summary>
        /// Gets a document that reads its data from two root selections.
        /// </summary>
        public Utf8OperationDocument TwoRootFieldDocument => _twoRootFieldDocument;

        public SourceSchemaClientRequest CreateLookupRequest()
            => new()
            {
                Node = _operationPlan.RootNodes[0],
                SchemaName = "A",
                OperationType = OperationType.Query,
                OperationSourceText = Encoding.UTF8.GetBytes(Lookup),
                OperationHash = 1,
                Variables = [CreateVariableValues("1"), CreateVariableValues("2")],
                Document = _document,
                LookupTypeName = "Foo"
            };

        public VariableValues CreateVariableValues(string id)
            => new(CompactPath.Root, CreateSegment(_memory, $$"""{"__fusion_1_id":"{{id}}"}"""));

        /// <summary>
        /// Creates the variables of the client request that the plan is executed with.
        /// </summary>
        public VariableValueCollection CreateClientVariables(string name)
        {
            var schema = (FusionSchemaDefinition)_executor.Schema;
            var stringType = new NonNullType(schema.Types.GetType<IScalarTypeDefinition>("String"));

            return new VariableValueCollection(
                new Dictionary<string, VariableValue>(StringComparer.Ordinal)
                {
                    [name] = new(name, stringType, new StringValueNode("client"))
                });
        }

        /// <summary>
        /// Gets the body of the last request the client sent.
        /// </summary>
        public string Body => _handler.Body!;

        public Task<string> SendAsync(SourceSchemaClientRequest request)
            => SendAsync(request, VariableValueCollection.Empty);

        public async Task<string> SendAsync(
            SourceSchemaClientRequest request,
            VariableValueCollection variables)
        {
            var context = CreateContext(variables);

            await foreach (var result in _client.ExecuteAsync(
                context,
                request,
                TestContext.Current.CancellationToken))
            {
                result.Dispose();
            }

            return _handler.Body!;
        }

        /// <summary>
        /// Executes a request and returns its first result, leaving the remaining results of the
        /// request unread.
        /// </summary>
        public async Task<SourceSchemaResult> ReadFirstAsync(SourceSchemaClientRequest request)
        {
            var context = CreateContext(VariableValueCollection.Empty);

            await foreach (var result in _client.ExecuteAsync(
                context,
                request,
                TestContext.Current.CancellationToken))
            {
                return result;
            }

            throw new InvalidOperationException("The request produced no result.");
        }

        public Task<List<int>> SendBatchAsync(ImmutableArray<SourceSchemaClientRequest> requests)
            => SendBatchAsync(requests, VariableValueCollection.Empty);

        /// <summary>
        /// Executes a batch and returns the request index of every result it produced.
        /// </summary>
        public async Task<List<int>> SendBatchAsync(
            ImmutableArray<SourceSchemaClientRequest> requests,
            VariableValueCollection variables)
        {
            var context = CreateContext(variables);
            var requestIndices = new List<int>();

            await foreach (var result in _client.ExecuteBatchAsync(
                context,
                requests,
                TestContext.Current.CancellationToken))
            {
                requestIndices.Add(result.RequestIndex);
                result.Result.Dispose();
            }

            return requestIndices;
        }

        private OperationPlanContext CreateContext(VariableValueCollection variables)
        {
            var contextPool = _executor.Schema.Services.GetRequiredService<OperationPlanContextPool>();
            var context = contextPool.Rent();
            var cts = new CancellationTokenSource();
            var requestContextPool =
                _executor.Schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
            var requestContext = requestContextPool.Get();

            requestContext.Initialize(
                _executor.Schema,
                _executor.Version,
                OperationRequestBuilder.New().SetDocument("{ field }").Build(),
                requestIndex: 0,
                requestServices: _services,
                requestAborted: CancellationToken.None);

            context.Initialize(
                requestContext,
                variables,
                _operationPlan,
                cts,
                new MemoryArena());

            _ctsList.Add(cts);
            _requestContexts.Add((requestContextPool, requestContext));
            _contexts.Add(context);
            return context;
        }

        public async ValueTask DisposeAsync()
        {
            await _client.DisposeAsync();

            foreach (var context in _contexts)
            {
                await context.DisposeAsync();
            }

            foreach (var (pool, requestContext) in _requestContexts)
            {
                pool.Return(requestContext);
            }

            foreach (var cts in _ctsList)
            {
                cts.Dispose();
            }

            _document.Dispose();
            _twoRootFieldDocument.Dispose();
            _memory.Dispose();
            await _services.DisposeAsync();
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly List<string> _bodies = [];

        public string? Body => _bodies.Count == 0 ? null : _bodies[^1];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"data":{"_0_fieldById":{"field":"A"},"_1_fieldById":{"field":"B"}}}""",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
