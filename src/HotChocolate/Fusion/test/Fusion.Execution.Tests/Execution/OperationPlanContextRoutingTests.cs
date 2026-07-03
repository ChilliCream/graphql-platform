using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using FusionNameNode = HotChocolate.Fusion.Language.NameNode;
using IntValueNode = HotChocolate.Language.IntValueNode;
using StringValueNode = HotChocolate.Language.StringValueNode;
using ObjectFieldNode = HotChocolate.Language.ObjectFieldNode;
using IValueNode = HotChocolate.Language.IValueNode;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanContextRoutingTests : FusionTestBase
{
    private static readonly FusionSchemaDefinition s_schema = CreateCompositeSchema();

    [Fact]
    public async Task CreateVariableValueSets_Should_RouteThroughResultStore_When_RequirementKeysIsNull()
    {
        // arrange
        await using var fixture = await RoutingTestFixture.CreateAsync();
        var context = fixture.CreateContext();

        // act
        var result = context.CreateVariableValueSets(
            SelectionPath.Root,
            forwardedVariables: [],
            requirements: [Requirement("__fusion_1_id")]);

        // assert
        Assert.True(result.IsDefaultOrEmpty);
    }

    [Fact]
    public async Task CreateVariableValueSets_Should_RouteThroughResultStore_When_RequestedRequirementsHaveNoOverlap()
    {
        // arrange
        await using var fixture = await RoutingTestFixture.CreateAsync();
        var context = fixture.CreateContext();
        fixture.SetRequirements(context, ImportedKeys("__fusion_1_id"), Field("__fusion_1_id", new StringValueNode("1")));

        // act
        var result = context.CreateVariableValueSets(
            SelectionPath.Root,
            forwardedVariables: [],
            requirements: [Requirement("__fusion_2_other")]);

        // assert
        Assert.True(result.IsDefaultOrEmpty);
    }

    [Fact]
    public async Task CreateVariableValueSets_Should_Throw_When_RequestedRequirementsPartiallyOverlap()
    {
        // arrange
        await using var fixture = await RoutingTestFixture.CreateAsync();
        var context = fixture.CreateContext();
        fixture.SetRequirements(context, ImportedKeys("__fusion_1_id"), Field("__fusion_1_id", new StringValueNode("1")));

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => context.CreateVariableValueSets(
                SelectionPath.Root,
                forwardedVariables: [],
                requirements:
                [
                    Requirement("__fusion_1_id"),
                    Requirement("__fusion_2_other")
                ]));

        // assert
        Assert.Contains("__fusion_1_id", exception.Message);
        Assert.Contains("__fusion_2_other", exception.Message);
    }

    [Fact]
    public async Task Complete_Should_SealArena_When_ResultIsCompleted()
    {
        // arrange
        // A completed buffered result no longer writes into its arena, so the arena is sealed and its
        // pages are returned to the pool when the result is disposed.
        await using var fixture = await RoutingTestFixture.CreateAsync();
        var context = fixture.CreateContext();
        var arena = context.Memory;

        // act
        context.Complete();

        // assert
        Assert.True(arena.IsSealed);
    }

    [Fact]
    public async Task Complete_Should_SealEventArena_When_SubscriptionEventIsCompleted()
    {
        // arrange
        // Each subscription event is backed by its own arena; once the event result is completed
        // nothing writes into that arena, so it is sealed and its pages can be returned on dispose.
        await using var fixture = await RoutingTestFixture.CreateAsync();
        var context = fixture.CreateContext();
        var eventArena = new MemoryArena();
        context.SetActiveEventArena(eventArena);

        // act
        context.Complete(reusable: true);

        // assert
        Assert.True(eventArena.IsSealed);
    }

    [Fact]
    public async Task CreateVariableValueSets_Should_ReturnImportedSnapshotWholesale_When_FullExactMatchAndNoForwardedVariables()
    {
        // arrange
        await using var fixture = await RoutingTestFixture.CreateAsync();
        var context = fixture.CreateContext();
        fixture.SetRequirements(
            context,
            ImportedKeys("__fusion_1_id", "__fusion_2_sku"),
            Field("__fusion_1_id", new StringValueNode("1")),
            Field("__fusion_2_sku", new StringValueNode("sku-1")));

        // act
        var first = context.CreateVariableValueSets(
            SelectionPath.Root,
            forwardedVariables: [],
            requirements:
            [
                Requirement("__fusion_1_id"),
                Requirement("__fusion_2_sku")
            ]);
        var second = context.CreateVariableValueSets(
            SelectionPath.Root,
            forwardedVariables: [],
            requirements:
            [
                Requirement("__fusion_1_id"),
                Requirement("__fusion_2_sku")
            ]);

        // assert
        // Case D returns the cached _requirementValues array wholesale, so two
        // calls hand back identical references (the imported snapshot itself).
        var entry = Assert.Single(first);
        Assert.True(first == second);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_1_id":"1","__fusion_2_sku":"sku-1"}
            """);
    }

    [Fact]
    public async Task CreateVariableValueSets_Should_CallSnapshotMergePath_When_FullMatchWithForwardedVariables()
    {
        // arrange
        await using var fixture = await RoutingTestFixture.CreateAsync(
            variables: new Dictionary<string, IValueNode>
            {
                ["limit"] = new IntValueNode(10)
            });
        var context = fixture.CreateContext();
        fixture.SetRequirements(
            context,
            ImportedKeys("__fusion_1_id"),
            Field("__fusion_1_id", new StringValueNode("1")));

        // act
        var result = context.CreateVariableValueSets(
            SelectionPath.Root,
            forwardedVariables: ["limit"],
            requirements: [Requirement("__fusion_1_id")]);

        // assert
        var entry = Assert.Single(result);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"limit":10,"__fusion_1_id":"1"}
            """);
    }

    [Fact]
    public async Task CreateVariableValueSets_Should_CallSnapshotMergePath_When_StrictSubsetWithoutForwardedVariables()
    {
        // arrange
        await using var fixture = await RoutingTestFixture.CreateAsync();
        var context = fixture.CreateContext();
        fixture.SetRequirements(
            context,
            ImportedKeys("__fusion_1_id", "__fusion_2_sku"),
            Field("__fusion_1_id", new StringValueNode("1")),
            Field("__fusion_2_sku", new StringValueNode("sku-1")));

        // act
        var result = context.CreateVariableValueSets(
            SelectionPath.Root,
            forwardedVariables: [],
            requirements: [Requirement("__fusion_1_id")]);

        // assert
        var entry = Assert.Single(result);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_1_id":"1"}
            """);
    }

    [Fact]
    public async Task CreateVariableValueSets_MultiSet_Should_Throw_When_RequestedRequirementsPartiallyOverlap()
    {
        // arrange
        await using var fixture = await RoutingTestFixture.CreateAsync();
        var context = fixture.CreateContext();
        fixture.SetRequirements(context, ImportedKeys("__fusion_1_id"), Field("__fusion_1_id", new StringValueNode("1")));

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => context.CreateVariableValueSets(
                selectionSets: [SelectionPath.Root],
                forwardedVariables: [],
                requiredData:
                [
                    Requirement("__fusion_1_id"),
                    Requirement("__fusion_2_other")
                ]));

        // assert
        Assert.Contains("__fusion_1_id", exception.Message);
        Assert.Contains("__fusion_2_other", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_When_RequestIsSubscription()
    {
        // arrange
        await using var fixture = await RoutingTestFixture.CreateAsync();
        var context = fixture.CreateContext();
        await using var client = new HttpSourceSchemaClient(
            GraphQLHttpClient.Create(new HttpClient()),
            new HttpSourceSchemaClientConfiguration("a", new Uri("http://localhost:5000/graphql")));

        var request = new SourceSchemaClientRequest
        {
            Node = fixture.GetRootNode(),
            SchemaName = "a",
            OperationType = OperationType.Subscription,
            OperationSourceText = "subscription { field }",
            OperationHash = 0
        };

        // act
        async Task Act()
        {
            await foreach (var _ in client.ExecuteAsync(context, request, TestContext.Current.CancellationToken))
            {
            }
        }

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Act);
        Assert.Contains("SubscribeAsync", exception.Message);
    }

    private static ObjectFieldNode Field(string name, IValueNode value)
        => new(name, value);

    private static OperationRequirement Requirement(string key)
        => new(
            key,
            new NamedTypeNode("String"),
            OperationRequirement.CreateInputType(new NamedTypeNode("String"), s_schema),
            SelectionPath.Root,
            new PathNode(new PathSegmentNode(new FusionNameNode(key))));

    private static HashSet<string> ImportedKeys(params string[] keys)
        => new(keys, StringComparer.Ordinal);

    private static string Normalize(JsonSegment segment)
    {
        using var document = JsonDocument.Parse(segment.AsSequence());
        return JsonSerializer.Serialize(document.RootElement);
    }

    private sealed class RoutingTestFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly IRequestExecutor _executor;
        private readonly OperationPlan _operationPlan;
        private readonly IVariableValueCollection _variables;
        private readonly List<OperationPlanContext> _rentedContexts = [];
        private readonly List<FetchResultStore> _stores = [];
        private readonly List<CancellationTokenSource> _ctsList = [];
        private readonly List<(ObjectPool<PooledRequestContext> Pool, PooledRequestContext Context)> _requestContexts = [];

        private RoutingTestFixture(
            ServiceProvider services,
            IRequestExecutor executor,
            OperationPlan operationPlan,
            IVariableValueCollection variables)
        {
            _services = services;
            _executor = executor;
            _operationPlan = operationPlan;
            _variables = variables;
        }

        public static async Task<RoutingTestFixture> CreateAsync(
            IReadOnlyDictionary<string, IValueNode>? variables = null)
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
            var schema = (Fusion.Types.FusionSchemaDefinition)executor.Schema;
            var operationPlan = PlanOperation(
                schema,
                """
                query {
                  field
                }
                """);

            var coercedVariables = new Dictionary<string, VariableValue>(StringComparer.Ordinal);
            var stringType = new NonNullType(schema.Types.GetType<IScalarTypeDefinition>("String"));

            if (variables is not null)
            {
                foreach (var (name, value) in variables)
                {
                    coercedVariables[name] = new VariableValue(name, stringType, value);
                }
            }

            return new RoutingTestFixture(
                services,
                executor,
                operationPlan,
                new VariableValueCollection(coercedVariables));
        }

        public ExecutionNode GetRootNode() => _operationPlan.RootNodes[0];

        public OperationPlanContext CreateContext()
        {
            var pool = _executor.Schema.Services.GetRequiredService<OperationPlanContextPool>();
            var context = pool.Rent();
            var cts = new CancellationTokenSource();
            _ctsList.Add(cts);

            var requestContextPool =
                _executor.Schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
            var requestContext = requestContextPool.Get();
            _requestContexts.Add((requestContextPool, requestContext));
            requestContext.Initialize(
                _executor.Schema,
                _executor.Version,
                new OperationRequest(
                    document: new OperationDocumentSourceText("{ field }"),
                    documentId: null,
                    documentHash: null,
                    operationName: null,
                    errorHandlingMode: null,
                    variableValues: null,
                    extensions: null,
                    contextData: null,
                    features: null,
                    services: _services,
                    flags: RequestFlags.AllowAll),
                requestIndex: 0,
                requestServices: _services,
                requestAborted: CancellationToken.None);

            // The request executor always attaches a memory arena to the request before a plan
            // runs. The arena is supplied explicitly here because this fixture does not go through
            // the request executor that would otherwise attach it to the request context.
            context.Initialize(requestContext, _variables, _operationPlan, cts, new MemoryArena());
            _rentedContexts.Add(context);
            return context;
        }

        public void SetRequirements(
            OperationPlanContext context,
            HashSet<string> keys,
            params ObjectFieldNode[] fields)
        {
            var sourceStore = new FetchResultStore();
            _stores.Add(sourceStore);

            var entry = sourceStore.CreateVariableValueSets(CompactPath.Root, fields);
            context.SetRequirements([entry], keys);
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

            foreach (var store in _stores)
            {
                store.Dispose();
            }

            foreach (var cts in _ctsList)
            {
                cts.Dispose();
            }

            await _services.DisposeAsync();
        }
    }
}
