using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanExecutor
{
    public async Task<IExecutionResult> ExecuteAsync(
        RequestContext requestContext,
        IVariableValueCollection variables,
        OperationPlan operationPlan,
        CancellationToken cancellationToken = default)
    {
        var context = new OperationPlanContext(requestContext, variables, operationPlan);
        context.Begin();
        var strategy = DetermineExecutionStrategy(context);
        await ExecutorSession.ExecuteAsync(context, strategy, cancellationToken);
        return context.Complete();
    }

    public async Task<IExecutionResult> SubscribeAsync(
        RequestContext requestContext,
        OperationPlan operationPlan,
        CancellationToken cancellationToken = default)
    {
        // subscription plans must have a single root,
        // which represents the subscription to a source schema.
        var root = operationPlan.RootNodes.Single();

        if (root is not OperationExecutionNode subscriptionNode)
        {
            // TODO : error handling
            throw new InvalidOperationException();
        }

        var context = new OperationPlanContext(requestContext, operationPlan);
        var subscription = await subscriptionNode.SubscribeAsync(context, cancellationToken);
        var strategy = DetermineExecutionStrategy(context);
        var session = new ExecutorSession(context, strategy, cancellationToken);

        if (subscription.Status is not ExecutionStatus.Success)
        {
            // TODO : error handling
            throw new InvalidOperationException();
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
                    await session.ExecuteAsync(subscriptionNode, eventArgs);
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

    private static ExecutionStrategy DetermineExecutionStrategy(OperationPlanContext context)
        => context.OperationPlan.Operation.Definition.Operation switch
        {
            OperationType.Query => ExecutionStrategy.Query,
            OperationType.Mutation => ExecutionStrategy.Mutation,
            OperationType.Subscription => ExecutionStrategy.Subscription,
            _ => throw new InvalidOperationException()
        };

    private sealed class ExecutorSession
    {
        private readonly List<ExecutionNode> _stack = [];
        private readonly HashSet<ExecutionNode> _completed = [];
        private readonly HashSet<Task<ExecutionNodeResult>> _activeTasks = [];
        private readonly List<Task<ExecutionNodeResult>> _completedTasks = [];
        private readonly List<ExecutionNode> _backlog;
        private readonly OperationPlanContext _context;
        private readonly OperationPlan _plan;
        private readonly ImmutableArray<ExecutionNodeTrace>.Builder? _traces;
        private readonly CancellationToken _cancellationToken;
        private readonly ExecutionStrategy _strategy;
        private int _nextRootNode;

        public ExecutorSession(
            OperationPlanContext context,
            ExecutionStrategy strategy,
            CancellationToken cancellationToken)
        {
            _context = context;
            _strategy = strategy;
            _cancellationToken = cancellationToken;
            _plan = context.OperationPlan;
            _backlog = [.. context.OperationPlan.AllNodes];

            // For sequential execution (mutations), remove root nodes from backlog initially
            if (_strategy is ExecutionStrategy.Mutation or ExecutionStrategy.Subscription)
            {
                foreach (var root in context.OperationPlan.RootNodes)
                {
                    _backlog.Remove(root);
                }
            }

            _traces = context.CollectTelemetry
                ? ImmutableArray.CreateBuilder<ExecutionNodeTrace>()
                : null;
        }

        public static Task ExecuteAsync(
            OperationPlanContext context,
            ExecutionStrategy strategy,
            CancellationToken cancellationToken)
            => new ExecutorSession(context, strategy, cancellationToken).ExecuteInternalAsync();

        public async Task ExecuteAsync(ExecutionNode node, EventMessageResult result)
        {
            // pre-seed state for the first node
            _completed.Add(node);
            _traces?.Add(new ExecutionNodeTrace
            {
                Id = node.Id,
                SpanId = result.Activity?.SpanId.ToHexString(),
                Status = result.Status,
                Duration = result.Duration
            });

            // process the rest of the nodes
            while (EnqueueNextNodes())
            {
                await WaitForNextCompletionAsync();
            }

            // add the traces to the context
            if (_traces is { Count: > 0 })
            {
                _context.Traces = [.. _traces];
            }

            // reset the state for the next execution
            _backlog.AddRange(_plan.AllNodes);

            foreach (var root in _plan.RootNodes)
            {
                _backlog.Remove(root);
            }

            _traces?.Clear();
            _stack.Clear();
            _completed.Clear();
            _activeTasks.Clear();
            _completedTasks.Clear();
        }

        private async Task ExecuteInternalAsync()
        {
            if (_strategy == ExecutionStrategy.Query)
            {
                await ExecuteQueryAsync();
            }
            else
            {
                await ExecuteMutationAsync();
            }

            if (_traces is { Count: > 0 })
            {
                _context.Traces = [.. _traces];
            }
        }

        private async Task ExecuteQueryAsync()
        {
            // Start all root nodes immediately for parallel execution
            StartAllRootNodes();

            // Process until all nodes complete
            while (IsProcessing())
            {
                await WaitForNextCompletionAsync();
                EnqueueNextNodes();
            }
        }

        private async Task ExecuteMutationAsync()
        {
            // Sequential root processing - one root at a time
            while (StartNextRootNode())
            {
                // Complete the entire subtree of current root before starting next
                var enqueued = true;
                while (enqueued)
                {
                    await WaitForNextCompletionAsync();
                    enqueued = EnqueueNextNodes();
                }
            }
        }

        private void StartAllRootNodes()
        {
            foreach (var node in _context.OperationPlan.RootNodes)
            {
                StartNode(node);
            }
        }

        private bool StartNextRootNode()
        {
            var roots = _context.OperationPlan.RootNodes;
            if (_nextRootNode < roots.Length)
            {
                StartNode(roots[_nextRootNode++]);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsProcessing() => _backlog.Count > 0 || _activeTasks.Count > 0;

        private void SkipNode(ExecutionNode node)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartNode(ExecutionNode node)
        {
            _backlog.Remove(node);
            _activeTasks.Add(node.ExecuteAsync(_context, _cancellationToken));
        }

        private async Task WaitForNextCompletionAsync()
        {
            await Task.WhenAny(_activeTasks);

            foreach (var task in _activeTasks)
            {
                if (task.IsCompletedSuccessfully)
                {
                    var node = _plan.GetNodeById(task.Result.Id);

                    _completedTasks.Add(task);
                    _completed.Add(node);
                    _traces?.Add(new ExecutionNodeTrace
                    {
                        Id = task.Result.Id,
                        SpanId = task.Result.Activity?.SpanId.ToHexString(),
                        Status = task.Result.Status,
                        Duration = task.Result.Duration
                    });

                    if (task.Result.Status is ExecutionStatus.Skipped or ExecutionStatus.Failed)
                    {
                        SkipNode(node);
                    }
                }
                else if (task.IsFaulted || task.IsCanceled)
                {
                    // execution nodes are not expected to throw as exception should be handled within.
                    // if they do it's a fatal error for the execution, so we await failed task here
                    // so that they can throw and terminate the execution.
                    await task;
                }
            }

            foreach (var task in _completedTasks)
            {
                _activeTasks.Remove(task);
            }

            _completedTasks.Clear();
        }

        private bool EnqueueNextNodes()
        {
            var enqueued = false;
            _stack.Clear();

            foreach (var node in _backlog)
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

                if (dependenciesFulfilled)
                {
                    _stack.Push(node);
                    enqueued = true;
                }
            }

            foreach (var node in _stack)
            {
                StartNode(node);
            }

            return enqueued;
        }
    }
}
