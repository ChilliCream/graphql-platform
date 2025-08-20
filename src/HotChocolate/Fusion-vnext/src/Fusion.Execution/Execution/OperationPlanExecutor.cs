using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Execution;

internal sealed class OperationPlanExecutor
{
    public async Task<IExecutionResult> ExecuteAsync(
        RequestContext requestContext,
        IVariableValueCollection variables,
        OperationPlan operationPlan,
        CancellationToken cancellationToken = default)
    {
        var context = new OperationPlanContext(requestContext, variables, operationPlan);
        context.Begin();

        switch (operationPlan.Operation.Definition.Operation)
        {
            case OperationType.Query:
                await ExecuteQueryAsync(context, operationPlan, context.ExecutionState, cancellationToken);
                break;

            case OperationType.Mutation:
                await ExecuteMutationAsync(context, operationPlan, context.ExecutionState, cancellationToken);
                break;

            default:
                throw new InvalidOperationException("Only queries and mutations can be executed.");
        }

        return context.Complete();
    }

    private static async Task ExecuteQueryAsync(
        OperationPlanContext context,
        OperationPlan plan,
        ExecutionState executionState,
        CancellationToken cancellationToken)
    {
        cancellationToken.Register(() => executionState.Signal.TryResetToIdle());

        // GraphQL queries allow us to execute the plan by using full parallelism.
        // We fill the backlog with all nodes from the operation plan.
        executionState.FillBacklog(plan);

        // Then we start all root nodes as they can be processed in parallel.
        foreach (var root in plan.RootNodes)
        {
            executionState.StartNode(context, root, cancellationToken);
        }

        while (!cancellationToken.IsCancellationRequested
            && executionState.IsProcessing())
        {
            while (executionState.TryDequeueCompletedResult(out var result))
            {
                var node = plan.GetNodeById(result.Id);
                executionState.CompleteNode(node, result);
            }

            executionState.EnqueueNextNodes(context, cancellationToken);

            if (cancellationToken.IsCancellationRequested
                || !executionState.IsProcessing())
            {
                break;
            }

            // The signal will be set every time a node completes and will release the executor
            // from the async wait to go through the completed results.
            await executionState.Signal;
        }

        if (context.CollectTelemetry)
        {
            context.Traces = [..executionState.Traces];
        }
    }

    private static async Task ExecuteMutationAsync(
        OperationPlanContext context,
        OperationPlan plan,
        ExecutionState executionState,
        CancellationToken cancellationToken)
    {
        cancellationToken.Register(() => executionState.Signal.TryResetToIdle());

        // For mutations, we fill the backlog with all nodes from the operation plan just like for queries.
        executionState.FillBacklog(plan);

        // The difference here is that the planner has one root node for each mutation field.
        // We execute the root nodes one after the other to cater for the GraphQL spec mutation algorithm
        // that requires sequential execution the roots but allows for parallel execution of their subtrees.
        foreach (var root in plan.RootNodes)
        {
            // We start the first root ...
            executionState.StartNode(context, root, cancellationToken);

            // ... and then process the subtree until its complete.
            // This is why in the mutation algorithm check for `HasActiveNodes` instead of `IsProcessing` which
            // would be true as long as there are items on the backlog. `HasActiveNodes` however will be true
            // as long as there are active nodes that result from processing the current subtree.
            while (!cancellationToken.IsCancellationRequested && executionState.HasActiveNodes())
            {
                while (executionState.TryDequeueCompletedResult(out var result))
                {
                    var node = plan.GetNodeById(result.Id);
                    executionState.CompleteNode(node, result);
                }

                executionState.EnqueueNextNodes(context, cancellationToken);

                if (cancellationToken.IsCancellationRequested
                    || !executionState.HasActiveNodes())
                {
                    break;
                }

                // The signal will be set every time a node completes and will release the executor
                // from the async wait to go through the completed results.
                await executionState.Signal;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        if (context.CollectTelemetry)
        {
            context.Traces = [..executionState.Traces];
        }
    }

    public async Task<IExecutionResult> SubscribeAsync(
        RequestContext requestContext,
        OperationPlan operationPlan,
        CancellationToken cancellationToken = default)
    {
        // subscription plans must have a single root,
        // which represents the subscription to a source schema.
        var root = operationPlan.RootNodes.Single();

        // In the case of a subscription the initial node must always be an operation node
        // that represents the subscription to a specific source schema.
        if (root is not OperationExecutionNode subscriptionNode)
        {
            throw new InvalidOperationException("The specified operation plan is not supported.");
        }

        var context = new OperationPlanContext(requestContext, operationPlan);
        var subscription = await subscriptionNode.SubscribeAsync(context, cancellationToken);
        var executionState = context.ExecutionState;
        var plan = context.OperationPlan;

        cancellationToken.Register(() => executionState.Signal.TryResetToIdle());

        if (subscription.Status is not ExecutionStatus.Success)
        {
            throw new InvalidOperationException("We could not subscribe to the underlying source schema.");
        }

        var stream = new ResponseStream(CreateResponseStream);
        stream.RegisterForCleanup(context);
        return stream;

        async IAsyncEnumerable<IOperationResult> CreateResponseStream()
        {
            await foreach (var eventArgs in subscription.ReadStreamAsync().WithCancellation(cancellationToken))
            {
                IOperationResult result;

                try
                {
                    context.Begin(eventArgs.StartTimestamp, eventArgs.Activity?.TraceId.ToHexString());

                    executionState.Reset();
                    executionState.FillBacklog(plan);
                    executionState.EnqueueForCompletion(
                        new ExecutionNodeResult(
                            subscriptionNode.Id,
                            eventArgs.Activity,
                            eventArgs.Status,
                            eventArgs.Duration,
                            null,
                            []));

                    while (!cancellationToken.IsCancellationRequested && executionState.IsProcessing())
                    {
                        while (executionState.TryDequeueCompletedResult(out var nodeResult))
                        {
                            var node = plan.GetNodeById(nodeResult.Id);
                            executionState.CompleteNode(node, nodeResult);
                        }

                        executionState.EnqueueNextNodes(context, cancellationToken);

                        if (cancellationToken.IsCancellationRequested
                            || !executionState.IsProcessing())
                        {
                            break;
                        }

                        // The signal will be set every time a node completes and will release the executor
                        // from the async wait to go through the completed results.
                        await executionState.Signal;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    result = context.Complete();
                }
                finally
                {
                    // disposing the eventArgs disposes the telemetry scope.
                    eventArgs.Dispose();
                }

                yield return result;
            }
        }
    }
}

internal sealed class ExecutionState
{
    private readonly List<ExecutionNode> _stack = [];

    private readonly List<ExecutionNode> _backlog = [];

    private readonly HashSet<ExecutionNode> _completed = [];

    private readonly ConcurrentQueue<ExecutionNodeResult> _completedResults = new();

    private int _activeNodes;

    public readonly List<ExecutionNodeTrace> Traces = [];

    public readonly AsyncAutoResetEvent Signal = new();

    public bool CollectTelemetry;

    public void FillBacklog(OperationPlan plan)
    {
        switch (plan.Operation.Definition.Operation)
        {
            case OperationType.Query:
                _backlog.AddRange(plan.AllNodes);
                break;

            case OperationType.Mutation:
                foreach (var node in plan.AllNodes)
                {
                    // we skip root nodes as they are enqueued by the algorithm
                    // one by one.
                    if (node.Dependencies.Length == 0)
                    {
                        continue;
                    }

                    _backlog.Add(node);
                }
                break;

            case OperationType.Subscription:
                _backlog.AddRange(plan.AllNodes);
                _backlog.Remove(plan.RootNodes.Single());

                // The root node of a subscription is started outside the state.
                // We cater to this fact and fix the state by stating with am active node count of 1.
                Interlocked.Increment(ref _activeNodes);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    "Unexpected operation type.");
        }
    }

    public void Reset()
    {
        _stack.Clear();
        _backlog.Clear();
        _completed.Clear();
        _completedResults.Clear();
        _activeNodes = 0;

        Traces.Clear();
        Signal.TryResetToIdle();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsProcessing() => _backlog.Count > 0 || Volatile.Read(ref _activeNodes) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasActiveNodes() => Volatile.Read(ref _activeNodes) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartNode(OperationPlanContext context, ExecutionNode node, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _activeNodes);
        _backlog.Remove(node);
        node.ExecuteAsync(context, cancellationToken).FireAndForget();
    }

    public void EnqueueForCompletion(ExecutionNodeResult result)
    {
        _completedResults.Enqueue(result);
        Signal.Set();
    }

    public bool TryDequeueCompletedResult([NotNullWhen(true)] out ExecutionNodeResult? result)
        => _completedResults.TryDequeue(out result);

    public void CompleteNode(ExecutionNode node, ExecutionNodeResult result)
    {
        Interlocked.Decrement(ref _activeNodes);
        _completed.Add(node);

        if (CollectTelemetry)
        {
            Traces.Add(new ExecutionNodeTrace
            {
                Id = result.Id,
                SpanId = result.Activity?.SpanId.ToHexString(),
                Status = result.Status,
                Duration = result.Duration
            });
        }

        if (result.DependentsToExecute.Length > 0)
        {
            foreach (var dependent in node.Dependents)
            {
                if (!result.DependentsToExecute.Contains(dependent))
                {
                    SkipNode(dependent);
                }
            }
        }

        if (result.Status is ExecutionStatus.Skipped or ExecutionStatus.Failed)
        {
            SkipNode(node);
        }
    }

    public void SkipNode(ExecutionNode node)
    {
        _stack.Clear();
        _stack.Push(node);

        while (_stack.TryPop(out var current))
        {
            _backlog.Remove(current);

            foreach (var enqueuedNode in _backlog)
            {
                if (enqueuedNode.Dependencies.Contains(current))
                {
                    _stack.Push(enqueuedNode);
                }
            }
        }
    }

    public bool EnqueueNextNodes(OperationPlanContext context, CancellationToken cancellationToken)
    {
        _stack.Clear();

        foreach (var node in _backlog)
        {
            if (CanExecuteNode(node))
            {
                _stack.Push(node);
            }
        }

        foreach (var node in _stack)
        {
            StartNode(context, node, cancellationToken);
        }

        return _stack.Count > 0;
    }

    private bool CanExecuteNode(ExecutionNode node)
    {
        var dependenciesFulfilled = true;

        foreach (var dependency in node.Dependencies)
        {
            if (_completed.Contains(dependency))
            {
                continue;
            }

            dependenciesFulfilled = false;
        }

        return dependenciesFulfilled;
    }
}
