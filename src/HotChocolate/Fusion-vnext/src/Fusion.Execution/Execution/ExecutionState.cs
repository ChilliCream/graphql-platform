using System.Collections.Concurrent;
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

    private byte[] _nodeStates = [];
    private int[] _remainingDependencies = [];
    private int _backlogCount;
    private int _activeNodes;
    private bool _readyIsSorted = true;

    public readonly OrderedDictionary<int, ExecutionNodeTrace> Traces = [];
    public readonly AsyncAutoResetEvent Signal = new();

    public void FillBacklog(OperationPlan plan)
    {
        _ready.Clear();
        _readyIsSorted = true;
        _backlogCount = 0;

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
        _readyIsSorted = true;
        _backlogCount = 0;

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

        if ((uint)node.Id < (uint)_remainingDependencies.Length)
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
                    AddReadyNode(dependent);
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
                if ((uint)dependent.Id >= (uint)_remainingDependencies.Length
                    || _remainingDependencies[dependent.Id] < 0)
                {
                    continue;
                }

                if (IsInBacklog(dependent.Id))
                {
                    _stack.Push(dependent);
                }
            }
        }
    }

    public bool EnqueueNextNodes(OperationPlanContext context, CancellationToken cancellationToken)
    {
        var readyCount = _ready.Count;

        if (readyCount == 0)
        {
            return false;
        }

        if (_readyIsSorted)
        {
            var enqueuedAny = false;

            for (var i = 0; i < readyCount; i++)
            {
                var node = _ready[i];

                if ((uint)node.Id < (uint)_remainingDependencies.Length
                    && _remainingDependencies[node.Id] == 0)
                {
                    StartNode(context, node, cancellationToken);
                    enqueuedAny = true;
                }
            }

            _ready.Clear();
            _readyIsSorted = true;
            return enqueuedAny;
        }

        _stack.Clear();

        for (var i = 0; i < readyCount; i++)
        {
            var node = _ready[i];

            if ((uint)node.Id < (uint)_remainingDependencies.Length
                && _remainingDependencies[node.Id] == 0)
            {
                _stack.Push(node);
            }
        }

        _ready.Clear();
        _readyIsSorted = true;

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
        => (uint)nodeId < (uint)_nodeStates.Length
            && _nodeStates[nodeId] == NodeStateBacklog;

    private void AddToBacklog(ExecutionNode node)
    {
        var nodeId = node.Id;

        if ((uint)nodeId >= (uint)_nodeStates.Length)
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

        var remainingDependencies = node.Dependencies.Length;
        EnsureDependencyCapacity(nodeId + 1);
        _remainingDependencies[nodeId] = remainingDependencies;
        _trackedDependencySlots.Add(nodeId);

        if (remainingDependencies == 0)
        {
            AddReadyNode(node);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddReadyNode(ExecutionNode node)
    {
        var readyCount = _ready.Count;

        if (_readyIsSorted
            && readyCount > 0
            && _ready[readyCount - 1].Id > node.Id)
        {
            _readyIsSorted = false;
        }

        _ready.Add(node);
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
