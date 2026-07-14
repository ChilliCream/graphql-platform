using System.Collections.Immutable;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanContextPoolingTests : FusionTestBase
{
    [Fact]
    public async Task Pool_Should_Clear_Large_Plan_State_Before_Smaller_Plan_Reuse()
    {
        await using var fixture = await PoolingTestFixture.CreateAsync();
        var (largePlan, nodeField, fallback, batch, batchDefinition) =
            CreateLargeCapabilityPlan(fixture.DefaultOperation);
        var (smallPlan, smallNode, smallDependent) = CreateSmallPlan(fixture.DefaultOperation);
        var batchError = new InvalidOperationException("large batch failed");
        ImmutableArray<VariableValues> variableValueSets = [VariableValues.Empty];
        var transportUri = new Uri("https://large.example/graphql");
        OperationPlanContext pooledContext;

        Assert.True(largePlan.UsesDynamicSchemaNames);
        Assert.True(largePlan.UsesBatchNodes);
        Assert.False(smallPlan.UsesDynamicSchemaNames);
        Assert.False(smallPlan.UsesBatchNodes);
        Assert.True(largePlan.MaxNodeId > smallPlan.MaxNodeId);

        await using (var large = fixture.Rent())
        {
            large.Initialize(largePlan);
            large.Context.Begin();
            large.Context.EnqueueForExecution(fallback, nodeField);
            large.Context.SetDynamicSchemaName(fallback, "large");
            large.Context.TrackSkippedDefinition(batch, batchDefinition);
            large.Context.TrackBatchRequestError(batch, 3, batchError);
            large.Context.TrackVariableValueSets(fallback, variableValueSets);
            large.Context.TrackTransport(fallback, transportUri, "application/json");

            Assert.Collection(
                large.Context.GetDependentsToExecute(fallback),
                dependent => Assert.Same(nodeField, dependent));
            Assert.Equal("large", large.Context.GetDynamicSchemaName(fallback));
            Assert.Collection(
                large.Context.GetSkippedDefinitions(batch),
                definition => Assert.Same(batchDefinition, definition));
            Assert.True(large.Context.TryGetBatchRequestError(batch, 3, out var trackedError));
            Assert.Same(batchError, trackedError);
            Assert.Collection(
                large.Context.GetVariableValueSets(fallback),
                values => Assert.Equal(VariableValues.Empty, values));
            var trackedTransport = large.Context.GetTransportDetails(fallback);
            Assert.Same(transportUri, trackedTransport.Uri);
            Assert.Equal("application/json", trackedTransport.ContentType);

            pooledContext = large.Context;
        }

        await using var small = fixture.Rent();
        Assert.Same(pooledContext, small.Context);

        small.Initialize(smallPlan);
        small.Context.Begin();

        Assert.Empty(small.Context.GetDependentsToExecute(fallback));
        Assert.Throws<InvalidOperationException>(() => small.Context.GetDynamicSchemaName(fallback));
        Assert.Empty(small.Context.GetSkippedDefinitions(batch));
        Assert.False(small.Context.TryGetBatchRequestError(batch, 3, out var staleError));
        Assert.Null(staleError);
        Assert.Empty(small.Context.GetVariableValueSets(fallback));
        var staleTransport = small.Context.GetTransportDetails(fallback);
        Assert.Null(staleTransport.Uri);
        Assert.Null(staleTransport.ContentType);
        Assert.Empty(small.Context.GetDependentsToExecute(smallNode));

        small.Context.EnqueueForExecution(smallNode, smallDependent);

        Assert.Collection(
            small.Context.GetDependentsToExecute(smallNode),
            dependent => Assert.Same(smallDependent, dependent));
    }

    [Fact]
    public async Task Pool_Should_Return_Context_When_Initialize_Fails_Before_Active_Bound_Is_Published()
    {
        await using var fixture = await PoolingTestFixture.CreateAsync();
        var (smallPlan, smallNode, smallDependent) = CreateSmallPlan(fixture.DefaultOperation);
        var failingPlan = CreateFailingLargePlan(fixture.ConditionalOperation);
        OperationPlanContext pooledContext;

        Assert.True(failingPlan.MaxNodeId > smallPlan.MaxNodeId);

        await using (var initial = fixture.Rent())
        {
            initial.Initialize(smallPlan);
            initial.Context.Begin();
            pooledContext = initial.Context;
        }

        await using (var failing = fixture.Rent())
        {
            Assert.Same(pooledContext, failing.Context);

            var exception = Assert.Throws<InvalidOperationException>(
                () => failing.Initialize(failingPlan));

            Assert.Equal("The variable include has an invalid value.", exception.Message);
        }

        await using var recovered = fixture.Rent();
        Assert.Same(pooledContext, recovered.Context);

        recovered.Initialize(smallPlan);
        recovered.Context.Begin();

        Assert.Empty(recovered.Context.GetDependentsToExecute(smallNode));

        recovered.Context.EnqueueForExecution(smallNode, smallDependent);

        Assert.Collection(
            recovered.Context.GetDependentsToExecute(smallNode),
            dependent => Assert.Same(smallDependent, dependent));
    }

    private static (
        OperationPlan Plan,
        NodeFieldExecutionNode NodeField,
        TestExecutionNode Fallback,
        OperationBatchExecutionNode Batch,
        SingleOperationDefinition BatchDefinition) CreateLargeCapabilityPlan(Operation operation)
    {
        var nodeField = new NodeFieldExecutionNode(
            64,
            "node",
            new StringValueNode("node-id"),
            []);
        var fallback = new TestExecutionNode(65);
        nodeField.AddFallbackQuery(fallback);

        var batchDefinition = new SingleOperationDefinition(
            127,
            new OperationSourceText(
                "BatchOperation",
                OperationType.Query,
                "query BatchOperation { field }",
                "batch-operation"),
            "a",
            SelectionPath.Root,
            SelectionPath.Root,
            [],
            [],
            ResultSelectionSet.Create(new SelectionSetNode([new FieldNode("field")])),
            [],
            requiresFileUpload: false);
        var batch = new OperationBatchExecutionNode(126, [batchDefinition]);

        var plan = OperationPlan.Create(
            "large-capability-plan",
            operation,
            [nodeField],
            [nodeField, fallback, batch],
            deliveryGroups: [],
            incrementalPlans: [],
            searchSpace: 0,
            expandedNodes: 0);

        return (plan, nodeField, fallback, batch, batchDefinition);
    }

    private static (OperationPlan Plan, TestExecutionNode Node, TestExecutionNode Dependent)
        CreateSmallPlan(Operation operation)
    {
        var node = new TestExecutionNode(0);
        var dependent = new TestExecutionNode(1);
        var plan = OperationPlan.Create(
            "small-plan",
            operation,
            [node],
            [node, dependent],
            deliveryGroups: [],
            incrementalPlans: [],
            searchSpace: 0,
            expandedNodes: 0);

        return (plan, node, dependent);
    }

    private static OperationPlan CreateFailingLargePlan(Operation operation)
    {
        var node = new TestExecutionNode(255);
        return OperationPlan.Create(
            "failing-large-plan",
            operation,
            [node],
            [node],
            deliveryGroups: [],
            incrementalPlans: [],
            searchSpace: 0,
            expandedNodes: 0);
    }

    private sealed class TestExecutionNode(int id) : ExecutionNode
    {
        public override int Id { get; } = id;

        public override ExecutionNodeType Type => ExecutionNodeType.Operation;

        public override ReadOnlySpan<ExecutionNodeCondition> Conditions => [];

        public override string SchemaName => "a";

        protected override ValueTask<ExecutionStatus> OnExecuteAsync(
            OperationPlanContext context,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ExecutionStatus.Success);
    }

    private sealed class PoolingTestFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly IRequestExecutor _executor;
        private readonly OperationPlanContextPool _contextPool;
        private readonly ObjectPool<PooledRequestContext> _requestContextPool;

        private PoolingTestFixture(
            ServiceProvider services,
            IRequestExecutor executor,
            OperationPlanContextPool contextPool,
            ObjectPool<PooledRequestContext> requestContextPool,
            Operation defaultOperation,
            Operation conditionalOperation)
        {
            _services = services;
            _executor = executor;
            _contextPool = contextPool;
            _requestContextPool = requestContextPool;
            DefaultOperation = defaultOperation;
            ConditionalOperation = conditionalOperation;
        }

        public Operation DefaultOperation { get; }

        public Operation ConditionalOperation { get; }

        public static async Task<PoolingTestFixture> CreateAsync()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            var gatewayBuilder = serviceCollection
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(
                    ComposeSchemaDocument(
                        """
                        # name: a
                        type Query {
                          field: String!
                        }
                        """));
            gatewayBuilder.ModifyRequestOptions(
                options => options.CollectOperationPlanTelemetry = true);
            var services = serviceCollection.BuildServiceProvider();
            var executor = await services.GetRequestExecutorAsync();
            var schema = (FusionSchemaDefinition)executor.Schema;
            var defaultOperation = PlanOperation(schema, "query { field }").Operation;
            var conditionalOperation = PlanOperation(
                schema,
                "query WithInclude($include: Boolean!) { field @include(if: $include) }").Operation;

            return new PoolingTestFixture(
                services,
                executor,
                executor.Schema.Services.GetRequiredService<OperationPlanContextPool>(),
                executor.Schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>(),
                defaultOperation,
                conditionalOperation);
        }

        public ContextRental Rent()
        {
            var context = _contextPool.Rent();
            var requestContext = _requestContextPool.Get();
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

            return new ContextRental(
                context,
                requestContext,
                _requestContextPool,
                new CancellationTokenSource(),
                new MemoryArena());
        }

        public async ValueTask DisposeAsync()
            => await _services.DisposeAsync();
    }

    private sealed class ContextRental(
        OperationPlanContext context,
        PooledRequestContext requestContext,
        ObjectPool<PooledRequestContext> requestContextPool,
        CancellationTokenSource cancellationTokenSource,
        MemoryArena memory) : IAsyncDisposable
    {
        public OperationPlanContext Context { get; } = context;

        public void Initialize(
            OperationPlan plan,
            IVariableValueCollection? variables = null)
            => Context.Initialize(
                requestContext,
                variables ?? VariableValueCollection.Empty,
                plan,
                cancellationTokenSource,
                memory);

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Context.DisposeAsync();
            }
            finally
            {
                requestContextPool.Return(requestContext);
                cancellationTokenSource.Dispose();
                memory.Seal();
                memory.Dispose();
            }
        }
    }
}
