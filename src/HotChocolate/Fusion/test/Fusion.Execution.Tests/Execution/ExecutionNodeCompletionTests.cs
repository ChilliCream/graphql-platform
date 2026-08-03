using System.Diagnostics;
using System.Reflection;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public sealed class ExecutionNodeCompletionTests : FusionTestBase
{
    [Fact]
    public async Task ExecuteAsync_Should_CaptureTelemetry_When_Enabled()
    {
        var disabledNode = new TestExecutionNode(0, TestOutcome.Success, delayMilliseconds: 10);
        disabledNode.Seal();

        await using (var fixture = await ExecutionTestFixture.CreateAsync(false, disabledNode))
        {
            fixture.State.FillBacklog(fixture.Plan);

            using var activity = new Activity("telemetry-disabled");
            activity.Start();

            var result = await ExecuteNodeAsync(fixture, disabledNode, CancellationToken.None);
            fixture.State.CompleteNode(fixture.Plan, disabledNode, result);

            Assert.Null(result.Activity);
            Assert.Equal(TimeSpan.Zero, result.Duration);
            Assert.Empty(fixture.State.Traces);
        }

        var enabledNode = new TestExecutionNode(0, TestOutcome.Success, delayMilliseconds: 10);
        enabledNode.Seal();

        await using (var fixture = await ExecutionTestFixture.CreateAsync(true, enabledNode))
        {
            fixture.State.FillBacklog(fixture.Plan);

            using var activity = new Activity("telemetry-enabled");
            activity.Start();

            var result = await ExecuteNodeAsync(fixture, enabledNode, CancellationToken.None);
            fixture.State.CompleteNode(fixture.Plan, enabledNode, result);

            var trace = Assert.Single(fixture.State.Traces).Value;
            Assert.Same(activity, result.Activity);
            Assert.True(result.Duration > TimeSpan.Zero);
            Assert.Equal(activity.SpanId.ToHexString(), trace.SpanId);
            Assert.Equal(result.Duration, trace.Duration);
        }
    }

    [Fact]
    public async Task ExecuteAsync_Should_CompleteExactlyOnce_When_OutcomeVaries()
    {
        var failure = new InvalidOperationException("execution failed");
        var nodes = new[]
        {
            new TestExecutionNode(0, TestOutcome.Success),
            new TestExecutionNode(1, TestOutcome.Skipped),
            new TestExecutionNode(2, TestOutcome.Failed, failure),
            new TestExecutionNode(3, TestOutcome.Canceled)
        };

        foreach (var node in nodes)
        {
            node.Seal();
        }

        await using var fixture = await ExecutionTestFixture.CreateAsync(false, nodes);
        fixture.State.FillBacklog(fixture.Plan);

        var expected = new[]
        {
            (ExecutionStatus.Success, (Exception?)null, CancellationToken.None),
            (ExecutionStatus.Skipped, (Exception?)null, CancellationToken.None),
            (ExecutionStatus.Failed, (Exception?)failure, CancellationToken.None),
            (ExecutionStatus.Failed, (Exception?)null, new CancellationToken(canceled: true))
        };

        for (var i = 0; i < nodes.Length; i++)
        {
            var result = await ExecuteNodeAsync(fixture, nodes[i], expected[i].Item3);

            Assert.Equal(expected[i].Item1, result.Status);

            if (nodes[i].Outcome is TestOutcome.Canceled)
            {
                Assert.IsType<OperationCanceledException>(result.Exception);
            }
            else
            {
                Assert.Same(expected[i].Item2, result.Exception);
            }

            fixture.State.CompleteNode(fixture.Plan, nodes[i], result);

            Assert.False(fixture.State.HasActiveNodes());
            Assert.False(fixture.State.TryDequeueCompletedResult(out _));
        }

        Assert.False(fixture.State.IsProcessing());
    }

    [Fact]
    public async Task CompleteNode_Should_ScheduleSelectedDependentAndPreserveSkippedDefinitions_When_ResultSelectsDependent()
    {
        var source = new TestExecutionNode(0, TestOutcome.Success);
        var selected = new TestExecutionNode(1, TestOutcome.Success);
        var skipped = new TestExecutionNode(2, TestOutcome.Success);

        source.AddDependent(selected);
        source.AddDependent(skipped);
        selected.AddDependency(source);
        skipped.AddDependency(source);
        source.DependentToExecute = selected;
        source.SkippedDefinition = skipped;

        source.Seal();
        selected.Seal();
        skipped.Seal();

        await using var fixture = await ExecutionTestFixture.CreateAsync(false, source, selected, skipped);
        fixture.State.FillBacklog(fixture.Plan);

        var sourceResult = await ExecuteNodeAsync(fixture, source, CancellationToken.None);

        Assert.Equal(ExecutionStatus.Success, sourceResult.Status);
        Assert.Collection(sourceResult.DependentsToExecute, node => Assert.Same(selected, node));
        Assert.Collection(sourceResult.SkippedDefinitions, node => Assert.Same(skipped, node));

        fixture.State.CompleteNode(fixture.Plan, source, sourceResult);

        Assert.True(fixture.State.IsNodeSkipped(skipped.Id));
        Assert.True(fixture.State.EnqueueNextNodes(fixture.Context, CancellationToken.None));

        var selectedResult = await WaitForCompletedResultAsync(fixture.State);
        fixture.State.CompleteNode(fixture.Plan, selected, selectedResult);

        Assert.Equal(selected.Id, selectedResult.Id);
        Assert.Equal(1, selected.ExecutionCount);
        Assert.Equal(0, skipped.ExecutionCount);
        Assert.False(fixture.State.IsProcessing());
    }

    [Fact]
    public void ApplyPendingMergeFailure_Should_CopyStructWithFailureStatus_When_NodeHasPendingMergeFailure()
    {
        var state = new ExecutionState();
        var exception = new InvalidOperationException("merge failed");
        var result = new ExecutionNodeResult(
            7,
            Activity: null,
            ExecutionStatus.Success,
            TimeSpan.FromMilliseconds(3),
            Exception: null,
            DependentsToExecute: [],
            SkippedDefinitions: [],
            VariableValueSets: [],
            TransportDetails: (new Uri("https://example.com/graphql"), "application/json"));

        state.EnqueueForCompletion(result);

        Assert.True(state.TryDequeueCompletedResult(out var dequeued));
        Assert.Equal(result, dequeued);

        var mergeFailuresField = typeof(ExecutionState).GetField(
            "_mergeFailures",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find the merge-failure store.");
        mergeFailuresField.SetValue(
            state,
            new Dictionary<int, Exception?> { [result.Id] = exception });

        var failed = state.ApplyPendingMergeFailure(result);

        Assert.Equal(
            result with
            {
                Status = ExecutionStatus.Failed,
                Exception = exception
            },
            failed);
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Null(result.Exception);
    }

    private static async Task<ExecutionNodeResult> ExecuteNodeAsync(
        ExecutionTestFixture fixture,
        TestExecutionNode node,
        CancellationToken cancellationToken)
    {
        fixture.State.StartNode(fixture.Context, node, cancellationToken);
        return await WaitForCompletedResultAsync(fixture.State);
    }

    private static async Task<ExecutionNodeResult> WaitForCompletedResultAsync(ExecutionState state)
    {
        ExecutionNodeResult result;

        while (!state.TryDequeueCompletedResult(out result))
        {
            await state.Signal;
        }

        return result;
    }

    private enum TestOutcome
    {
        Success,
        Skipped,
        Failed,
        Canceled
    }

    private sealed class TestExecutionNode : ExecutionNode
    {
        private readonly Exception? _failure;
        private readonly int _delayMilliseconds;
        private int _executionCount;

        public TestExecutionNode(
            int id,
            TestOutcome outcome,
            Exception? failure = null,
            int delayMilliseconds = 0)
        {
            Id = id;
            Outcome = outcome;
            _failure = failure;
            _delayMilliseconds = delayMilliseconds;
        }

        public override int Id { get; }

        public override ExecutionNodeType Type => ExecutionNodeType.Operation;

        public override ReadOnlySpan<ExecutionNodeCondition> Conditions => [];

        public override string SchemaName => "test";

        public TestOutcome Outcome { get; }

        public int ExecutionCount => _executionCount;

        public ExecutionNode? DependentToExecute { get; set; }

        public IOperationPlanNode? SkippedDefinition { get; set; }

        protected override bool IsSkipped(OperationPlanContext context)
            => Outcome is TestOutcome.Skipped;

        protected override async ValueTask<ExecutionStatus> OnExecuteAsync(
            OperationPlanContext context,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _executionCount);

            if (_delayMilliseconds > 0)
            {
                await Task.Delay(_delayMilliseconds, cancellationToken);
            }
            else
            {
                await Task.Yield();
            }

            if (DependentToExecute is { } dependent)
            {
                EnqueueDependentForExecution(context, dependent);
            }

            if (SkippedDefinition is { } skippedDefinition)
            {
                context.TrackSkippedDefinition(this, skippedDefinition);
            }

            return Outcome switch
            {
                TestOutcome.Success => ExecutionStatus.Success,
                TestOutcome.Failed => throw _failure ?? new InvalidOperationException("execution failed"),
                TestOutcome.Canceled => throw new OperationCanceledException(cancellationToken),
                _ => throw new InvalidOperationException("The skipped node must not execute.")
            };
        }

        protected override void OnError(
            OperationPlanContext context,
            IDisposable? scope,
            Exception error)
        {
        }
    }

    private sealed class ExecutionTestFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly ObjectPool<PooledRequestContext> _requestContextPool;
        private readonly PooledRequestContext _requestContext;
        private readonly CancellationTokenSource _cts;

        private ExecutionTestFixture(
            ServiceProvider services,
            ObjectPool<PooledRequestContext> requestContextPool,
            PooledRequestContext requestContext,
            CancellationTokenSource cts,
            OperationPlanContext context,
            OperationPlan plan)
        {
            _services = services;
            _requestContextPool = requestContextPool;
            _requestContext = requestContext;
            _cts = cts;
            Context = context;
            Plan = plan;
        }

        public OperationPlanContext Context { get; }

        public OperationPlan Plan { get; }

        public ExecutionState State => Context.ExecutionState;

        public static async Task<ExecutionTestFixture> CreateAsync(
            bool collectTelemetry,
            params TestExecutionNode[] nodes)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            var gatewayBuilder = serviceCollection
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(
                    ComposeSchemaDocument(
                        """
                        type Query {
                          field: String!
                        }
                        """));
            gatewayBuilder.ModifyRequestOptions(
                options => options.CollectOperationPlanTelemetry = collectTelemetry);

            var services = serviceCollection.BuildServiceProvider();
            var executor = await services.GetRequestExecutorAsync();
            var schema = (FusionSchemaDefinition)executor.Schema;
            var operation = PlanOperation(schema, "query { field }").Operation;
            var plan = OperationPlan.Create(
                "execution-node-completion-test",
                operation,
                [nodes[0]],
                [.. nodes],
                deliveryGroups: [],
                incrementalPlans: [],
                searchSpace: 0,
                expandedNodes: 0);
            var contextPool = executor.Schema.Services.GetRequiredService<OperationPlanContextPool>();
            var context = contextPool.Rent();
            var cts = new CancellationTokenSource();
            var requestContextPool =
                executor.Schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
            var requestContext = requestContextPool.Get();
            var request = OperationRequestBuilder.New()
                .SetDocument("{ field }")
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
                cts,
                new MemoryArena());

            return new ExecutionTestFixture(
                services,
                requestContextPool,
                requestContext,
                cts,
                context,
                plan);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            _requestContextPool.Return(_requestContext);
            _cts.Dispose();
            await _services.DisposeAsync();
        }
    }
}
