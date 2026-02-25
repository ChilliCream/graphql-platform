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

    private readonly List<ExecutionNode> _ready = [];

    private readonly List<ExecutionNode> _backlog = [];

    private readonly HashSet<ExecutionNode> _completed = [];

    private readonly Dictionary<ExecutionNode, int> _remainingDependencies = [];

    private readonly ConcurrentQueue<ExecutionNodeResult> _completedResults = new();

    private int _activeNodes;

    public readonly OrderedDictionary<int, ExecutionNodeTrace> Traces = [];

    public readonly AsyncAutoResetEvent Signal = new();

    public void FillBacklog(OperationPlan plan)
    {
        _ready.Clear();
        _remainingDependencies.Clear();

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
                throw new ArgumentOutOfRangeException("Unexpected operation type.");
        }

        foreach (var node in _backlog)
        {
            var remainingDependencies = node.Dependencies.Length;
            _remainingDependencies[node] = remainingDependencies;

            if (remainingDependencies == 0)
            {
                _ready.Add(node);
            }
        }
    }

    public void Reset()
    {
        _stack.Clear();
        _ready.Clear();
        _backlog.Clear();
        _completed.Clear();
        _remainingDependencies.Clear();
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
        _remainingDependencies.Remove(node);
        _backlog.Remove(node);
        _ = node.ExecuteAsync(context, cancellationToken);
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

    public void CompleteNode(
        OperationPlanContext context,
        ExecutionNode node,
        ExecutionNodeResult result)
    {
        Interlocked.Decrement(ref _activeNodes);

        if (collectTelemetry)
        {
            Traces.TryAdd(
                result.Id,
                new ExecutionNodeTrace
                {
                    Id = result.Id,
                    SpanId = result.Activity?.SpanId.ToHexString(),
                    Status = result.Status,
                    Duration = result.Duration,
                    VariableSets = result.VariableValueSets,
                    Transport = result.TransportDetails.Uri is not null
                        && result.TransportDetails.ContentType is not null
                            ? new ExecutionNodeTransportTrace
                            {
                                Uri = result.TransportDetails.Uri,
                                ContentType = result.TransportDetails.ContentType
                            }
                            : null
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
                        SkipNode(context, dependent);
                    }
                }
            }

            foreach (var dependent in node.Dependents)
            {
                if (!_remainingDependencies.TryGetValue(dependent, out var remainingDependencies))
                {
                    continue;
                }

                if (remainingDependencies == 1)
                {
                    _remainingDependencies[dependent] = 0;
                    _ready.Add(dependent);
                }
                else if (remainingDependencies > 1)
                {
                    _remainingDependencies[dependent] = remainingDependencies - 1;
                }
            }
        }

        if (result.Status is ExecutionStatus.Skipped or ExecutionStatus.Failed)
        {
            SkipNode(context, node);
        }
    }

    public void SkipNode(OperationPlanContext context, ExecutionNode node)
    {
        _stack.Clear();
        _stack.Push(node);

        while (_stack.TryPop(out var current))
        {
            context.SourceSchemaDispatcher.SkipNode(current.Id);
            _remainingDependencies.Remove(current);

            if (_backlog.Remove(current)
                && collectTelemetry
                && !_completed.Contains(current)
                && !Traces.ContainsKey(current.Id))
            {
                Traces.Add(
                    current.Id,
                    new ExecutionNodeTrace
                    {
                        Id = current.Id,
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

        if (_ready.Count == 0)
        {
            return false;
        }

        foreach (var node in _ready)
        {
            if (_remainingDependencies.TryGetValue(node, out var remainingDependencies)
                && remainingDependencies == 0)
            {
                _stack.Push(node);
            }
        }

        _ready.Clear();

        if (_stack.Count == 0)
        {
            return false;
        }

        _stack.Sort(static (a, b) => a.Id.CompareTo(b.Id));

        foreach (var node in _stack)
        {
            StartNode(context, node, cancellationToken);
        }

        return true;
    }
}
