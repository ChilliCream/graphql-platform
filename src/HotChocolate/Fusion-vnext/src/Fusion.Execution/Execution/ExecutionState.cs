using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Execution;

internal sealed class ExecutionState(bool collectTelemetry, CancellationTokenSource cts)
{
    private readonly List<ExecutionNode> _stack = [];

    private readonly List<ExecutionNode> _backlog = [];

    private readonly HashSet<ExecutionNode> _completed = [];

    private readonly ConcurrentQueue<ExecutionNodeResult> _completedResults = new();

    private int _activeNodes;

    public readonly List<ExecutionNodeTrace> Traces = [];

    public readonly AsyncAutoResetEvent Signal = new();

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

    public void CancelProcessing()
    {
        if (!cts.IsCancellationRequested)
        {
            cts.Cancel();
        }
    }

    public void CompleteNode(ExecutionNode node, ExecutionNodeResult result)
    {
        Interlocked.Decrement(ref _activeNodes);

        if (collectTelemetry)
        {
            Traces.Add(new ExecutionNodeTrace
            {
                Id = result.Id,
                SpanId = result.Activity?.SpanId.ToHexString(),
                Status = result.Status,
                Duration = result.Duration,
                VariableSets = result.VariableValueSets
            });
        }

        if (result.Status is ExecutionStatus.Success or ExecutionStatus.PartialSuccess)
        {
            _completed.Add(node);

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
            if (_backlog.Remove(current)
                && collectTelemetry
                && !_completed.Contains(current))
            {
                Traces.Add(new ExecutionNodeTrace
                {
                    Id = node.Id,
                    Status = ExecutionStatus.Skipped,
                    Duration = TimeSpan.Zero,
                    VariableSets = []
                });
            }

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
