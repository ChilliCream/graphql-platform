using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanExecutorWakeupTests : FusionTestBase
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ExecuteAsync_Should_CompleteEveryNode_When_NodesCompleteSynchronously()
    {
        // arrange
        const int requestsPerPlan = 160;
        const int concurrency = 8;
        await using var fixture = await WakeupTestFixture.CreateAsync();
        var counters = new NodeCounters();
        var plans = new[]
        {
            CreateRandomPlan(fixture.Operation, counters, seed: 1, nodeCount: 48, rootCount: 6),
            CreateRandomPlan(fixture.Operation, counters, seed: 2, nodeCount: 64, rootCount: 1),
            CreateRandomPlan(fixture.Operation, counters, seed: 3, nodeCount: 32, rootCount: 16)
        };
        var results = new SortedSet<string>(StringComparer.Ordinal);
        var expectedExecutions = 0;

        // act
        foreach (var plan in plans)
        {
            fixture.Plan = plan;
            expectedExecutions += requestsPerPlan * plan.AllNodes.Length;

            for (var batch = 0; batch < requestsPerPlan / concurrency; batch++)
            {
                var requests = new Task<string>[concurrency];

                for (var i = 0; i < concurrency; i++)
                {
                    requests[i] = fixture.ExecuteAsync(TestContext.Current.CancellationToken);
                }

                foreach (var result in await Task.WhenAll(requests)
                    .WaitAsync(s_timeout, TestContext.Current.CancellationToken))
                {
                    results.Add(result);
                }
            }
        }

        // assert
        Assert.Equal(expectedExecutions, counters.Started);
        Assert.Equal(expectedExecutions, counters.Finished);
        Assert.Single(results).MatchInlineSnapshot(
            """
            {
              "data": {
                "field": null
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_DrainActiveNodes_When_RequestIsCanceledWhileWaiting()
    {
        // arrange
        const int iterations = 300;
        await using var fixture = await WakeupTestFixture.CreateAsync();
        var counters = new NodeCounters();
        fixture.Plan = CreateCancellationPlan(fixture.Operation, counters);
        var results = new SortedSet<string>(StringComparer.Ordinal);
        var activeNodesAfterRequest = new List<int>(iterations);

        // act
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            counters.BeginIteration();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            var request = fixture.ExecuteAsync(cts.Token);

            await counters.Waiting.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            await PauseAsync(iteration);
            await cts.CancelAsync();
            await PauseAsync(iteration / 4);
            counters.ReleaseGate();

            results.Add(await request.WaitAsync(s_timeout, TestContext.Current.CancellationToken));
            activeNodesAfterRequest.Add(counters.Started - counters.Finished);
        }

        // assert
        Assert.Equal(Enumerable.Repeat(0, iterations), activeNodesAfterRequest);
        Assert.Single(results).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The GraphQL request execution was canceled.",
                  "extensions": {
                    "code": "HC0049"
                  }
                }
              ]
            }
            """);
    }

    private static async Task PauseAsync(int variant)
    {
        switch (variant % 4)
        {
            case 0:
                break;

            case 1:
                await Task.Yield();
                break;

            case 2:
                Thread.SpinWait(500);
                break;

            default:
                await Task.Delay(1);
                break;
        }
    }

    private static OperationPlan CreateRandomPlan(
        Operation operation,
        NodeCounters counters,
        int seed,
        int nodeCount,
        int rootCount)
    {
        var random = new Random(seed);
        var nodes = new WakeupTestNode[nodeCount];

        for (var i = 0; i < nodeCount; i++)
        {
            // most nodes complete synchronously so they signal while the executor is still busy.
            var behavior = random.Next(4) == 0
                ? NodeBehavior.Yield
                : NodeBehavior.CompleteSynchronously;
            nodes[i] = new WakeupTestNode(i + 1, behavior, counters);

            if (i < rootCount)
            {
                continue;
            }

            var first = random.Next(i);
            Connect(nodes[first], nodes[i]);

            var second = random.Next(i);

            if (second != first && random.Next(2) == 0)
            {
                Connect(nodes[second], nodes[i]);
            }
        }

        return CreatePlan(operation, nodes);
    }

    private static OperationPlan CreateCancellationPlan(Operation operation, NodeCounters counters)
    {
        var waitsForCancellation = new WakeupTestNode(1, NodeBehavior.WaitForCancellation, counters);
        var ignoresCancellation = new WakeupTestNode(2, NodeBehavior.IgnoreCancellation, counters);
        var nodes = new List<WakeupTestNode> { waitsForCancellation, ignoresCancellation };

        for (var i = 0; i < 8; i++)
        {
            var root = new WakeupTestNode(nodes.Count + 1, NodeBehavior.CompleteSynchronously, counters);
            nodes.Add(root);

            var child = new WakeupTestNode(
                nodes.Count + 1,
                i % 2 == 0 ? NodeBehavior.CompleteSynchronously : NodeBehavior.Yield,
                counters);
            nodes.Add(child);
            Connect(root, child);
        }

        var afterCancellation = new WakeupTestNode(nodes.Count + 1, NodeBehavior.CompleteSynchronously, counters);
        nodes.Add(afterCancellation);
        Connect(waitsForCancellation, afterCancellation);

        var afterGate = new WakeupTestNode(nodes.Count + 1, NodeBehavior.CompleteSynchronously, counters);
        nodes.Add(afterGate);
        Connect(ignoresCancellation, afterGate);

        return CreatePlan(operation, [.. nodes]);
    }

    private static void Connect(WakeupTestNode dependency, WakeupTestNode dependent)
    {
        dependent.AddDependency(dependency);
        dependency.AddDependent(dependent);
    }

    private static OperationPlan CreatePlan(Operation operation, WakeupTestNode[] nodes)
    {
        foreach (var node in nodes)
        {
            node.Seal();
        }

        ImmutableArray<ExecutionNode> allNodes = [.. nodes];

        return OperationPlan.Create(
            "executor-wakeup-test",
            operation,
            [.. allNodes.Where(static n => n.Dependencies.Length == 0)],
            allNodes,
            deliveryGroups: [],
            incrementalPlans: [],
            searchSpace: 0,
            expandedNodes: 0);
    }

    private enum NodeBehavior
    {
        CompleteSynchronously,
        Yield,
        WaitForCancellation,
        IgnoreCancellation
    }

    private sealed class NodeCounters
    {
        private int _started;
        private int _finished;
        private TaskCompletionSource _waiting = NewSignal();
        private TaskCompletionSource _gate = NewSignal();

        public int Started => Volatile.Read(ref _started);

        public int Finished => Volatile.Read(ref _finished);

        public Task Waiting => _waiting.Task;

        public Task Gate => _gate.Task;

        public void BeginIteration()
        {
            _waiting = NewSignal();
            _gate = NewSignal();
        }

        public void OnStarted() => Interlocked.Increment(ref _started);

        public void OnFinished() => Interlocked.Increment(ref _finished);

        public void OnWaiting() => _waiting.TrySetResult();

        public void ReleaseGate() => _gate.TrySetResult();

        private static TaskCompletionSource NewSignal()
            => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class WakeupTestNode(int id, NodeBehavior behavior, NodeCounters counters) : ExecutionNode
    {
        public override int Id { get; } = id;

        public override ExecutionNodeType Type => ExecutionNodeType.Operation;

        public override ReadOnlySpan<ExecutionNodeCondition> Conditions => [];

        public override string SchemaName => "test";

        protected override ValueTask<ExecutionStatus> OnExecuteAsync(
            OperationPlanContext context,
            CancellationToken cancellationToken = default)
        {
            counters.OnStarted();

            if (behavior is NodeBehavior.CompleteSynchronously)
            {
                counters.OnFinished();
                return new ValueTask<ExecutionStatus>(ExecutionStatus.Success);
            }

            return ExecuteAsynchronouslyAsync(cancellationToken);
        }

        private async ValueTask<ExecutionStatus> ExecuteAsynchronouslyAsync(CancellationToken cancellationToken)
        {
            try
            {
                switch (behavior)
                {
                    case NodeBehavior.Yield:
                        await Task.Yield();
                        break;

                    case NodeBehavior.WaitForCancellation:
                        counters.OnWaiting();
                        await Task.Delay(Timeout.Infinite, cancellationToken);
                        break;

                    case NodeBehavior.IgnoreCancellation:
                        await counters.Gate;
                        break;
                }

                return ExecutionStatus.Success;
            }
            finally
            {
                counters.OnFinished();
            }
        }

        protected override void OnError(
            OperationPlanContext context,
            IDisposable? scope,
            Exception error)
        {
        }
    }

    private sealed class WakeupTestFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly IRequestExecutor _executor;
        private readonly IOperationRequest _request;

        private WakeupTestFixture(
            ServiceProvider services,
            IRequestExecutor executor,
            Operation operation)
        {
            _services = services;
            _executor = executor;
            _request = OperationRequestBuilder.New()
                .SetDocument("{ field }")
                .Build();
            Operation = operation;
        }

        public Operation Operation { get; }

        public OperationPlan? Plan { get; set; }

        public static async Task<WakeupTestFixture> CreateAsync()
        {
            WakeupTestFixture? fixture = null;
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            serviceCollection
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(
                    ComposeSchemaDocument(
                        """
                        type Query {
                          field: String
                        }
                        """))
                .UseRequest(
                    next => context =>
                    {
                        // replace the planned operation with the test plan of the current fixture.
                        context.SetOperationPlan(fixture!.Plan!);
                        return next(context);
                    },
                    key: "WakeupTestPlan",
                    before: WellKnownRequestMiddleware.OperationExecutionMiddleware);

            var services = serviceCollection.BuildServiceProvider();
            var executor = await services.GetRequestExecutorAsync();
            var schema = (FusionSchemaDefinition)executor.Schema;
            var operation = PlanOperation(schema, "{ field }").Operation;
            fixture = new WakeupTestFixture(services, executor, operation);
            return fixture;
        }

        public async Task<string> ExecuteAsync(CancellationToken cancellationToken)
        {
            await using var result = await _executor.ExecuteAsync(_request, cancellationToken);
            return result.ToJson();
        }

        public ValueTask DisposeAsync() => _services.DisposeAsync();
    }
}
