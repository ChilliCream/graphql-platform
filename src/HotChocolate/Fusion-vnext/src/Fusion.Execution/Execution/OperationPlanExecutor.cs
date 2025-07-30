using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanExecutor
{
    public async Task<IExecutionResult> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        context.Begin();
        var strategy = DetermineExecutionStrategy(context);
        await ExecutorSession.ExecuteAsync(context, strategy, cancellationToken);
        return context.Complete();
    }

    private static ExecutionStrategy DetermineExecutionStrategy(OperationPlanContext context)
        => context.OperationPlan.Operation.Definition.Operation switch
        {
            OperationType.Mutation => ExecutionStrategy.SequentialRoots,
            _ => ExecutionStrategy.Parallel
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

        private ExecutorSession(OperationPlanContext context, ExecutionStrategy strategy, CancellationToken cancellationToken)
        {
            _context = context;
            _strategy = strategy;
            _cancellationToken = cancellationToken;
            _plan = context.OperationPlan;
            _backlog = [.. context.OperationPlan.AllNodes];

            // For sequential execution (mutations), remove root nodes from backlog initially
            if (_strategy == ExecutionStrategy.SequentialRoots)
            {
                foreach (var root in context.OperationPlan.RootNodes)
                {
                    _backlog.Remove(root);
                }
            }

            var collectTracing = context.Schema.GetRequestOptions().CollectOperationPlanTelemetry;
            _traces = collectTracing ? ImmutableArray.CreateBuilder<ExecutionNodeTrace>() : null;
        }

        public static Task ExecuteAsync(OperationPlanContext context, ExecutionStrategy strategy, CancellationToken cancellationToken)
            => new ExecutorSession(context, strategy, cancellationToken).ExecuteInternalAsync();

        private async Task ExecuteInternalAsync()
        {
            if (_strategy == ExecutionStrategy.Parallel)
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
