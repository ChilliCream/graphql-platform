using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class ExecutionState(bool collectTelemetry, CancellationTokenSource cts)
{
    private const byte NodeStateNone = 0;
    private const byte NodeStateBacklog = 1;
    private const byte NodeStateSkipped = 2;

    private readonly List<ExecutionNode> _stack = [];
    private readonly List<ExecutionNode> _ready = [];
    private readonly List<int> _trackedNodeStateSlots = [];
    private readonly List<int> _trackedDependencySlots = [];
    private readonly ConcurrentQueue<ExecutionNodeResult> _completedResults = new();
    private readonly HashSet<int> _failedOrSkippedNodes = [];

    private byte[] _nodeStates = [];
    private int[] _remainingDependencies = [];
    private int _backlogCount;
    private int _activeNodes;

    public readonly OrderedDictionary<int, ExecutionNodeTrace> Traces = [];
    public readonly AsyncAutoResetEvent Signal = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNodeSkipped(int nodeId)
        => _failedOrSkippedNodes.Contains(nodeId);

    public void FillBacklog(OperationPlan plan)
    {
        _ready.Clear();
        _backlogCount = 0;
        _failedOrSkippedNodes.Clear();

        ResetNodeStates();
        ResetRemainingDependencies();

        switch (plan.Operation.Definition.Operation)
        {
            case OperationType.Query:
                foreach (var node in plan.AllNodes)
                {
                    AddToBacklog(node);
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

                    AddToBacklog(node);
                }
                break;

            case OperationType.Subscription:
                var root = plan.RootNodes.Single();

                foreach (var node in plan.AllNodes)
                {
                    if (!ReferenceEquals(node, root))
                    {
                        AddToBacklog(node);
                    }
                }

                // The root node of a subscription is started outside the state.
                // We cater to this fact and fix the state by stating with am active node count of 1.
                Interlocked.Increment(ref _activeNodes);
                break;

            default:
                throw new ArgumentOutOfRangeException("Unexpected operation type.");
        }
    }

    public void Reset()
    {
        _stack.Clear();
        _ready.Clear();
        _backlogCount = 0;
        _failedOrSkippedNodes.Clear();

        ResetNodeStates();
        ResetRemainingDependencies();

        _completedResults.Clear();
        _activeNodes = 0;

        Traces.Clear();
        Signal.TryResetToIdle();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsProcessing() => _backlogCount > 0 || Volatile.Read(ref _activeNodes) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasActiveNodes() => Volatile.Read(ref _activeNodes) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartNode(OperationPlanContext context, ExecutionNode node, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _activeNodes);

        if (node.Id < _remainingDependencies.Length)
        {
            _remainingDependencies[node.Id] = -1;
        }

        RemoveFromBacklog(node.Id, NodeStateNone);
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
            // a node can explicitly choose which of its dependents should run
            // by calling EnqueueDependentForExecution during execution.
            // if it did, any dependent not in that list is skipped.
            if (result.DependentsToExecute.Length > 0)
            {
                var dependentsToExecute = result.DependentsToExecute;

                foreach (var dependent in node.Dependents)
                {
                    if (!ContainsDependent(dependentsToExecute, dependent))
                    {
                        SkipNode(context, dependent);
                    }
                }
            }

            // decrement the remaining dependency count for each dependent.
            // when a dependent's count reaches 0 all its dependencies are
            // fulfilled and it is ready to execute.
            foreach (var dependent in node.Dependents)
            {
                if (dependent.Id >= _remainingDependencies.Length)
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
            _failedOrSkippedNodes.Add(current.Id);
            context.SourceSchemaDispatcher.SkipNode(current.Id);

            if (current.Id < _remainingDependencies.Length)
            {
                _remainingDependencies[current.Id] = -1;
            }

            if (RemoveFromBacklog(current.Id, NodeStateSkipped)
                && collectTelemetry
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
                if (dependent.Id >= _remainingDependencies.Length
                    || _remainingDependencies[dependent.Id] < 0)
                {
                    continue;
                }

                if (!IsInBacklog(dependent.Id))
                {
                    continue;
                }

                // Fast path: no optional dependencies, use existing behavior.
                if (dependent.OptionalDependencies.Length == 0)
                {
                    _stack.Push(dependent);
                    continue;
                }

                // Check if the failed node is an optional dependency of the dependent.
                if (IsOptionalDependency(dependent, current))
                {
                    // Optional dependency failed: decrement counter but don't cascade skip.
                    var remaining = _remainingDependencies[dependent.Id];

                    if (remaining == 1)
                    {
                        _remainingDependencies[dependent.Id] = 0;

                        // All deps resolved. If the node has no required deps and all
                        // optional deps failed, skip it (nothing useful to execute).
                        if (ShouldSkipDueToAllOptionalDepsFailed(dependent))
                        {
                            _stack.Push(dependent);
                        }
                        else
                        {
                            _ready.Add(dependent);
                        }
                    }
                    else if (remaining > 1)
                    {
                        _remainingDependencies[dependent.Id] = remaining - 1;
                    }
                }
                else
                {
                    // Required dependency failed: cascade skip (existing behavior).
                    _stack.Push(dependent);
                }
            }
        }
    }

    private bool ShouldSkipDueToAllOptionalDepsFailed(ExecutionNode node)
    {
        // If the node has any required dependencies, it should not be skipped here.
        // Required deps that failed would have already cascaded a skip; if we reach
        // this point the required deps must have succeeded.
        if (node.Dependencies.Length > 0)
        {
            return false;
        }

        // All dependencies are optional. Check if every one of them failed or was skipped.
        foreach (var optDep in node.OptionalDependencies)
        {
            if (!_failedOrSkippedNodes.Contains(optDep.Id))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsOptionalDependency(ExecutionNode dependent, ExecutionNode dependency)
    {
        foreach (var optDep in dependent.OptionalDependencies)
        {
            if (ReferenceEquals(optDep, dependency))
            {
                return true;
            }
        }

        return false;
    }

    public bool EnqueueNextNodes(OperationPlanContext context, CancellationToken cancellationToken)
    {
        if (_ready.Count == 0)
        {
            return false;
        }

        var isSorted = true;
        var previousId = int.MinValue;
        var readyCount = _ready.Count;

        foreach (var node in _ready)
        {
            if (node.Id < _remainingDependencies.Length
                && _remainingDependencies[node.Id] == 0)
            {
                if (node.Id < previousId)
                {
                    isSorted = false;
                }

                previousId = node.Id;
            }
        }

        if (isSorted)
        {
            var enqueuedAny = false;

            for (var i = 0; i < readyCount; i++)
            {
                var node = _ready[i];

                if (node.Id < _remainingDependencies.Length
                    && _remainingDependencies[node.Id] == 0)
                {
                    StartNode(context, node, cancellationToken);
                    enqueuedAny = true;
                }
            }

            _ready.Clear();
            return enqueuedAny;
        }

        _stack.Clear();

        for (var i = 0; i < readyCount; i++)
        {
            var node = _ready[i];

            if (node.Id < _remainingDependencies.Length
                && _remainingDependencies[node.Id] == 0)
            {
                _stack.Push(node);
            }
        }

        _ready.Clear();

        if (_stack.Count == 0)
        {
            return false;
        }

        if (_stack.Count > 1)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsInBacklog(int nodeId)
        => nodeId < _nodeStates.Length
            && _nodeStates[nodeId] == NodeStateBacklog;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsDependent(
        ImmutableArray<ExecutionNode> dependentsToExecute,
        ExecutionNode dependent)
    {
        return dependentsToExecute.Length switch
        {
            1 => ReferenceEquals(dependentsToExecute[0], dependent),
            2 => ReferenceEquals(dependentsToExecute[0], dependent)
                || ReferenceEquals(dependentsToExecute[1], dependent),
            3 => ReferenceEquals(dependentsToExecute[0], dependent)
                || ReferenceEquals(dependentsToExecute[1], dependent)
                || ReferenceEquals(dependentsToExecute[2], dependent),
            _ => dependentsToExecute.Contains(dependent)
        };
    }

    private void AddToBacklog(ExecutionNode node)
    {
        var nodeId = node.Id;

        if (nodeId >= _nodeStates.Length)
        {
            EnsureNodeStateCapacity(nodeId + 1);
        }

        if (_nodeStates[nodeId] == NodeStateBacklog)
        {
            return;
        }

        if (_nodeStates[nodeId] == NodeStateNone)
        {
            _trackedNodeStateSlots.Add(nodeId);
        }

        _nodeStates[nodeId] = NodeStateBacklog;
        _backlogCount++;

        var remainingDependencies = node.Dependencies.Length + node.OptionalDependencies.Length;
        EnsureDependencyCapacity(nodeId + 1);
        _remainingDependencies[nodeId] = remainingDependencies;
        _trackedDependencySlots.Add(nodeId);

        if (remainingDependencies == 0)
        {
            _ready.Add(node);
        }
    }

    private bool RemoveFromBacklog(int nodeId, byte targetState)
    {
        if (!IsInBacklog(nodeId))
        {
            return false;
        }

        _nodeStates[nodeId] = targetState;
        _backlogCount--;
        return true;
    }

    private void ResetNodeStates()
    {
        if (_trackedNodeStateSlots.Count == 0)
        {
            return;
        }

        foreach (var slot in _trackedNodeStateSlots)
        {
            _nodeStates[slot] = NodeStateNone;
        }

        _trackedNodeStateSlots.Clear();
    }

    private void ResetRemainingDependencies()
    {
        if (_trackedDependencySlots.Count == 0)
        {
            return;
        }

        foreach (var slot in _trackedDependencySlots)
        {
            _remainingDependencies[slot] = -1;
        }

        _trackedDependencySlots.Clear();
    }

    private void EnsureNodeStateCapacity(int minCapacity)
    {
        if (_nodeStates.Length >= minCapacity)
        {
            return;
        }

        var newCapacity = _nodeStates.Length == 0 ? 8 : _nodeStates.Length;

        while (newCapacity < minCapacity)
        {
            newCapacity *= 2;
        }

        var nodeStates = new byte[newCapacity];

        if (_nodeStates.Length > 0)
        {
            Array.Copy(_nodeStates, nodeStates, _nodeStates.Length);
        }

        _nodeStates = nodeStates;
    }
}
