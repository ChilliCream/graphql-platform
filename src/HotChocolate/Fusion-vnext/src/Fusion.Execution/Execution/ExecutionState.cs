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

    private readonly HashSet<ExecutionNode> _backlog = [];

    private readonly HashSet<ExecutionNode> _completed = [];

    private readonly List<int> _trackedDependencySlots = [];

    private int[] _remainingDependencies = [];

    private readonly ConcurrentQueue<ExecutionNodeResult> _completedResults = new();

    private int _activeNodes;

    public readonly OrderedDictionary<int, ExecutionNodeTrace> Traces = [];

    public readonly AsyncAutoResetEvent Signal = new();

    public void FillBacklog(OperationPlan plan)
    {
        _ready.Clear();

        if (_trackedDependencySlots.Count > 0)
        {
            foreach (var slot in _trackedDependencySlots)
            {
                _remainingDependencies[slot] = -1;
            }

            _trackedDependencySlots.Clear();
        }

        switch (plan.Operation.Definition.Operation)
        {
            case OperationType.Query:
                foreach (var node in plan.AllNodes)
                {
                    _backlog.Add(node);
                }
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
                foreach (var node in plan.AllNodes)
                {
                    _backlog.Add(node);
                }

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
            EnsureDependencyCapacity(node.Id + 1);
            _remainingDependencies[node.Id] = remainingDependencies;
            _trackedDependencySlots.Add(node.Id);

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

        if (_trackedDependencySlots.Count > 0)
        {
            foreach (var slot in _trackedDependencySlots)
            {
                _remainingDependencies[slot] = -1;
            }

            _trackedDependencySlots.Clear();
        }

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

        if ((uint)node.Id < (uint)_remainingDependencies.Length)
        {
            _remainingDependencies[node.Id] = -1;
        }

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
                if ((uint)dependent.Id >= (uint)_remainingDependencies.Length)
                {
                    continue;
                }

                var remainingDependencies = _remainingDependencies[dependent.Id];

                if (remainingDependencies <= 0)
                {
                    continue;
                }

                if (remainingDependencies == 1)
                {
                    _remainingDependencies[dependent.Id] = 0;
                    _ready.Add(dependent);
                }
                else if (remainingDependencies > 1)
                {
                    _remainingDependencies[dependent.Id] = remainingDependencies - 1;
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

            if ((uint)current.Id < (uint)_remainingDependencies.Length)
            {
                _remainingDependencies[current.Id] = -1;
            }

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

            foreach (var dependent in current.Dependents)
            {
                if ((uint)dependent.Id >= (uint)_remainingDependencies.Length
                    || _remainingDependencies[dependent.Id] < 0)
                {
                    continue;
                }

                if (_backlog.Contains(dependent))
                {
                    _stack.Push(dependent);
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

        var isSorted = true;
        var previousId = int.MinValue;

        foreach (var node in _ready)
        {
            if ((uint)node.Id < (uint)_remainingDependencies.Length
                && _remainingDependencies[node.Id] == 0)
            {
                _stack.Push(node);

                if (node.Id < previousId)
                {
                    isSorted = false;
                }

                previousId = node.Id;
            }
        }

        _ready.Clear();

        if (_stack.Count == 0)
        {
            return false;
        }

        if (!isSorted && _stack.Count > 1)
        {
            _stack.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        }

        foreach (var node in _stack)
        {
            StartNode(context, node, cancellationToken);
        }

        return true;
    }

    private void EnsureDependencyCapacity(int minCapacity)
    {
        if (_remainingDependencies.Length >= minCapacity)
        {
            return;
        }

        var newCapacity = _remainingDependencies.Length == 0 ? 8 : _remainingDependencies.Length;

        while (newCapacity < minCapacity)
        {
            newCapacity *= 2;
        }

        var dependencies = new int[newCapacity];
        Array.Fill(dependencies, -1);

        if (_remainingDependencies.Length > 0)
        {
            Array.Copy(_remainingDependencies, dependencies, _remainingDependencies.Length);
        }

        _remainingDependencies = dependencies;
    }
}
