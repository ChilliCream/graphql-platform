using System.Collections.Immutable;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanContextResetTests : FusionTestBase
{
    private const string SchemaA =
        """
        # name: a
        type Query {
          foos: [Foo]
          bars: [Bar]
        }

        type Subscription {
          tick: String
        }

        type Foo @key(fields: "id") {
          id: ID!
        }

        type Bar @key(fields: "id") {
          id: ID!
        }
        """;

    private const string SchemaB =
        """
        # name: b
        type Query {
          fooById(id: ID! @is(field: "id")): Foo @lookup @internal
          barById(id: ID! @is(field: "id")): Bar @lookup @internal
        }

        type Foo @key(fields: "id") {
          id: ID!
          name: String
        }

        type Bar @key(fields: "id") {
          id: ID!
          title: String
        }
        """;

    private const string BatchOperation =
        """
        {
          foos {
            id
            name
          }
          bars {
            id
            title
          }
        }
        """;

    private static readonly FusionSchemaDefinition s_schema = ComposeSchema(SchemaA, SchemaB);

    [Fact]
    public void Plans_Should_DeriveResetCapabilities_When_NodeTypesVary()
    {
        var batchPlan = PlanOperation(s_schema, BatchOperation);
        var batchNode = GetOperationBatch(batchPlan);
        var nodeField = new NodeFieldExecutionNode(
            batchPlan.MaxNodeId + 1,
            "node",
            new StringValueNode("Rm9vOjE="),
            []);
        var apolloBatch = ApolloOperationBatchExecutionNode.Create(
            batchPlan.MaxNodeId + 2,
            GetSingleOperations(batchNode),
            s_schema);

        AssertCapabilities(batchPlan.Operation, [], false, false);
        AssertCapabilities(batchPlan.Operation, [nodeField], true, false);
        AssertCapabilities(batchPlan.Operation, [batchNode], false, true);
        AssertCapabilities(batchPlan.Operation, [apolloBatch], false, true);
        AssertCapabilities(batchPlan.Operation, [nodeField, batchNode], true, true);
    }

    [Fact]
    public async Task Begin_Should_ResetPerEventNodeState_When_ContextIsReusedForSubscriptionEvents()
    {
        await using var fixture = await ResetFixture.CreateAsync();
        var context = fixture.Context;
        var batchNode = fixture.BatchNode;
        var dynamicNode = fixture.DynamicNode;
        var skippedDefinition = batchNode.Operations[0];
        var firstError = new InvalidOperationException("first event failed");
        ImmutableArray<VariableValues> variableValueSets = [VariableValues.Empty];
        var firstTransportUri = new Uri("https://first.example/graphql");

        context.Begin();
        context.EnqueueForExecution(batchNode, dynamicNode);
        context.SetDynamicSchemaName(dynamicNode, "first-schema");
        context.TrackSkippedDefinition(batchNode, skippedDefinition);
        context.TrackBatchRequestError(batchNode, 0, firstError);
        context.TrackVariableValueSets(dynamicNode, variableValueSets);
        context.TrackTransport(dynamicNode, firstTransportUri, "application/json");

        Assert.Collection(
            context.GetDependentsToExecute(batchNode),
            dependent => Assert.Same(dynamicNode, dependent));
        Assert.Equal("first-schema", context.GetDynamicSchemaName(dynamicNode));
        Assert.Collection(
            context.GetSkippedDefinitions(batchNode),
            definition => Assert.Same(skippedDefinition, definition));
        Assert.True(context.TryGetBatchRequestError(batchNode, 0, out var storedFirstError));
        Assert.Same(firstError, storedFirstError);
        Assert.Collection(
            context.GetVariableValueSets(dynamicNode),
            values => Assert.Equal(VariableValues.Empty, values));
        var firstTransport = context.GetTransportDetails(dynamicNode);
        Assert.Same(firstTransportUri, firstTransport.Uri);
        Assert.Equal("application/json", firstTransport.ContentType);

        context.Begin();

        Assert.True(context.GetDependentsToExecute(batchNode).IsDefaultOrEmpty);
        Assert.Throws<InvalidOperationException>(() => context.GetDynamicSchemaName(dynamicNode));
        Assert.Empty(context.GetSkippedDefinitions(batchNode));
        Assert.False(context.TryGetBatchRequestError(batchNode, 0, out _));
        Assert.Empty(context.GetVariableValueSets(dynamicNode));
        var clearedTransport = context.GetTransportDetails(dynamicNode);
        Assert.Null(clearedTransport.Uri);
        Assert.Null(clearedTransport.ContentType);

        var secondError = new InvalidOperationException("second event failed");
        var secondTransportUri = new Uri("https://second.example/graphql");
        context.EnqueueForExecution(batchNode, dynamicNode);
        context.SetDynamicSchemaName(dynamicNode, "second-schema");
        context.TrackSkippedDefinition(batchNode, skippedDefinition);
        context.TrackBatchRequestError(batchNode, 0, secondError);
        context.TrackVariableValueSets(dynamicNode, variableValueSets);
        context.TrackTransport(dynamicNode, secondTransportUri, "application/graphql-response+json");

        Assert.Collection(
            context.GetDependentsToExecute(batchNode),
            dependent => Assert.Same(dynamicNode, dependent));
        Assert.Equal("second-schema", context.GetDynamicSchemaName(dynamicNode));
        Assert.Collection(
            context.GetSkippedDefinitions(batchNode),
            definition => Assert.Same(skippedDefinition, definition));
        Assert.True(context.TryGetBatchRequestError(batchNode, 0, out var storedSecondError));
        Assert.Same(secondError, storedSecondError);
        Assert.Collection(
            context.GetVariableValueSets(dynamicNode),
            values => Assert.Equal(VariableValues.Empty, values));
        var secondTransport = context.GetTransportDetails(dynamicNode);
        Assert.Same(secondTransportUri, secondTransport.Uri);
        Assert.Equal("application/graphql-response+json", secondTransport.ContentType);
    }

    private static void AssertCapabilities(
        Operation operation,
        ImmutableArray<ExecutionNode> nodes,
        bool usesDynamicSchemaNames,
        bool usesBatchNodes)
    {
        var rootNodes = nodes.IsDefaultOrEmpty
            ? ImmutableArray<ExecutionNode>.Empty
            : [nodes[0]];
        var plan = OperationPlan.Create(
            "reset-capability-test",
            operation,
            rootNodes,
            nodes,
            deliveryGroups: [],
            incrementalPlans: [],
            searchSpace: 0,
            expandedNodes: 0);
        var incrementalPlan = new IncrementalPlan(
            operation,
            rootNodes,
            nodes,
            deliveryGroups: [],
            requirements: []);

        Assert.Equal(usesDynamicSchemaNames, plan.UsesDynamicSchemaNames);
        Assert.Equal(usesBatchNodes, plan.UsesBatchNodes);
        Assert.Equal(usesDynamicSchemaNames, incrementalPlan.UsesDynamicSchemaNames);
        Assert.Equal(usesBatchNodes, incrementalPlan.UsesBatchNodes);
    }

    private static OperationBatchExecutionNode GetOperationBatch(OperationPlan plan)
    {
        foreach (var node in plan.AllNodes)
        {
            if (node is OperationBatchExecutionNode batchNode)
            {
                return batchNode;
            }
        }

        throw new InvalidOperationException("The plan does not contain an operation batch node.");
    }

    private static SingleOperationDefinition[] GetSingleOperations(OperationBatchExecutionNode batchNode)
    {
        var operations = new SingleOperationDefinition[batchNode.Operations.Length];

        for (var i = 0; i < operations.Length; i++)
        {
            operations[i] = Assert.IsType<SingleOperationDefinition>(batchNode.Operations[i]);
        }

        return operations;
    }

    private sealed class ResetFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly ObjectPool<PooledRequestContext> _requestContextPool;
        private readonly PooledRequestContext _requestContext;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly MemoryArena _memory;

        private ResetFixture(
            ServiceProvider services,
            ObjectPool<PooledRequestContext> requestContextPool,
            PooledRequestContext requestContext,
            CancellationTokenSource cancellationTokenSource,
            MemoryArena memory,
            OperationPlanContext context,
            OperationBatchExecutionNode batchNode,
            NodeFieldExecutionNode dynamicNode)
        {
            _services = services;
            _requestContextPool = requestContextPool;
            _requestContext = requestContext;
            _cancellationTokenSource = cancellationTokenSource;
            _memory = memory;
            Context = context;
            BatchNode = batchNode;
            DynamicNode = dynamicNode;
        }

        public OperationPlanContext Context { get; }

        public OperationBatchExecutionNode BatchNode { get; }

        public NodeFieldExecutionNode DynamicNode { get; }

        public static async Task<ResetFixture> CreateAsync()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            var gatewayBuilder = serviceCollection
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(ComposeSchemaDocument(SchemaA, SchemaB));
            gatewayBuilder.ModifyRequestOptions(
                options => options.CollectOperationPlanTelemetry = true);

            var services = serviceCollection.BuildServiceProvider();
            var executor = await services.GetRequestExecutorAsync();
            var schema = (FusionSchemaDefinition)executor.Schema;
            var batchPlan = PlanOperation(schema, BatchOperation);
            var subscriptionOperation = PlanOperation(schema, "subscription { tick }").Operation;
            var batchNode = GetOperationBatch(batchPlan);
            var dynamicNode = new NodeFieldExecutionNode(
                batchPlan.MaxNodeId + 1,
                "node",
                new StringValueNode("Rm9vOjE="),
                []);
            var allNodes = batchPlan.AllNodes.Add(dynamicNode);
            var plan = OperationPlan.Create(
                "subscription-reset-test",
                subscriptionOperation,
                batchPlan.RootNodes,
                allNodes,
                deliveryGroups: [],
                incrementalPlans: [],
                searchSpace: 0,
                expandedNodes: 0);
            var contextPool = executor.Schema.Services.GetRequiredService<OperationPlanContextPool>();
            var context = contextPool.Rent();
            var cancellationTokenSource = new CancellationTokenSource();
            var requestContextPool =
                executor.Schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
            var requestContext = requestContextPool.Get();
            var memory = new MemoryArena();
            var request = OperationRequestBuilder.New()
                .SetDocument("subscription { tick }")
                .Build();

            requestContext.Initialize(
                executor.Schema,
                executor.Version,
                request,
                requestIndex: 0,
                requestServices: services,
                requestAborted: CancellationToken.None);
            context.Initialize(
                requestContext,
                VariableValueCollection.Empty,
                plan,
                cancellationTokenSource,
                memory);

            return new ResetFixture(
                services,
                requestContextPool,
                requestContext,
                cancellationTokenSource,
                memory,
                context,
                batchNode,
                dynamicNode);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Context.DisposeAsync();
            }
            finally
            {
                _requestContextPool.Return(_requestContext);
                _cancellationTokenSource.Dispose();
                _memory.Seal();
                _memory.Dispose();
                await _services.DisposeAsync();
            }
        }
    }
}
