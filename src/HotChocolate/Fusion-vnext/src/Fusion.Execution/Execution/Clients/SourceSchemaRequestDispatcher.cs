using System.Runtime.InteropServices;
using HotChocolate.Language;
using static HotChocolate.Fusion.Execution.Clients.SourceSchemaClientCapabilities;
using static HotChocolate.Fusion.Properties.FusionExecutionResources;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Coordinates the dispatch of source schema requests, implementing both
/// <see cref="ISourceSchemaScheduler"/> and <see cref="ISourceSchemaDispatcher"/>.
/// <para>
/// Requests that do not belong to a batching group (or are subscriptions) are forwarded
/// directly to the underlying <see cref="ISourceSchemaClient"/>. Grouped requests are
/// held until every node in the group has submitted or been skipped, at which point they
/// are dispatched together via <see cref="ISourceSchemaClient.ExecuteBatchAsync"/>.
/// </para>
/// </summary>
internal sealed class SourceSchemaRequestDispatcher
    : ISourceSchemaScheduler
    , ISourceSchemaDispatcher
{
    private const int NodeStateUnregistered = -1;
    private const int NodeStatePending = 0;
    private const int NodeStateSubmitted = 1;
    private const int NodeStateSkipped = 2;

#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly OperationPlanContext _context;
    private readonly ISourceSchemaClientScope _clientScope;
    private readonly CancellationToken _requestAborted;
    private readonly Dictionary<int, GroupState> _groups = [];
    private readonly List<int> _trackedNodeIdSlots = [];
    private int[] _groupByNodeIdSlots = [];
    private int[] _nodeStateSlots = [];
    private Exception? _abortError;
    private bool _aborted;

    /// <summary>
    /// Initializes a new instance of <see cref="SourceSchemaRequestDispatcher"/>
    /// using the given <paramref name="context"/> to obtain the client scope and
    /// cancellation token for all downstream requests.
    /// </summary>
    /// <param name="context">
    /// The operation plan context that owns this dispatcher. The dispatcher uses
    /// <see cref="OperationPlanContext.ClientScope"/> to resolve clients and
    /// <see cref="HotChocolate.Execution.RequestContext.RequestAborted"/> to propagate cancellation.
    /// </param>
    public SourceSchemaRequestDispatcher(OperationPlanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _clientScope = context.ClientScope;
        _requestAborted = context.RequestContext.RequestAborted;
    }

    /// <summary>
    /// Executes a source schema request. If the request belongs to a batching group,
    /// it is held until all nodes in that group have submitted or been skipped, then
    /// dispatched as a batch. Otherwise, it is forwarded immediately.
    /// </summary>
    /// <param name="request">The source schema request to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response from the source schema.</returns>
    /// <exception cref="InvalidOperationException">
    /// The request's node was not registered in the expected batching group.
    /// </exception>
    public ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var client = _clientScope.GetClient(request.SchemaName, request.OperationType);

        // if the request is not part of a batch group,
        // if it is a mutation or subscription,
        // or if the source schema does not support request batching,
        // we will dispatch it right away without waiting for other requests.
        if ((client.Capabilities & RequestBatching) != RequestBatching
            || request.BatchingGroupId is not { } groupId
            || request.OperationType is OperationType.Mutation or OperationType.Subscription)
        {
            return client.ExecuteAsync(_context, request, cancellationToken);
        }

        PendingRequest? pendingRequest = null;
        List<PendingRequest>? pendingRequests = null;
        var needsDispatch = false;
        Exception? abortError = null;

        lock (_sync)
        {
            // the execution was aborted by the operation plan executor.
            if (_aborted)
            {
                abortError = CreateAbortException();
            }
            // we register the node to be dispatched.
            else if (_groups.TryGetValue(groupId, out var group)
                && group.TrySubmit(request, _nodeStateSlots, out pendingRequest))
            {
                if (group.TryCreateDispatch(out pendingRequests))
                {
                    needsDispatch = true;
                    RemoveGroup(group);
                }
            }
            // we are in an invalid state where the executor did not announce all groups or nodes.
            else
            {
                abortError = new InvalidOperationException(
                    string.Format(
                        SourceSchemaRequestDispatcher_NodeNotRegisteredInGroup,
                        request.Node.Id,
                        groupId));
            }
        }

        // now we handle the decisions we made in the lock.
        if (abortError is not null)
        {
            return ValueTask.FromException<SourceSchemaClientResponse>(abortError);
        }

        if (needsDispatch)
        {
            BeginDispatchGroup(pendingRequests!);
        }

        return new ValueTask<SourceSchemaClientResponse>(pendingRequest!.Completion.Task);
    }

    /// <summary>
    /// Registers a batching group with the given node IDs. All registered nodes must
    /// either submit a request via <see cref="ExecuteAsync"/> or be skipped via
    /// <see cref="SkipNode"/> before the group is dispatched.
    /// </summary>
    /// <param name="groupId">The batching group identifier.</param>
    /// <param name="nodeIds">The execution node IDs that belong to this group.</param>
    public void RegisterGroup(int groupId, IReadOnlyList<int> nodeIds)
    {
        ArgumentNullException.ThrowIfNull(nodeIds);

        if (nodeIds.Count == 0)
        {
            throw new ArgumentException(
                SourceSchemaRequestDispatcher_RegisterGroupEmptyNodeIds,
                nameof(nodeIds));
        }

        lock (_sync)
        {
            if (_aborted)
            {
                return;
            }

            if (!_groups.TryGetValue(groupId, out var group))
            {
                group = new GroupState(groupId, nodeIds.Count);
                _groups.Add(groupId, group);
            }

            foreach (var nodeId in nodeIds)
            {
                EnsureNodeIdSlotCapacity(nodeId + 1);
                var existingGroupId = _groupByNodeIdSlots[nodeId];

                if (existingGroupId < 0)
                {
                    _trackedNodeIdSlots.Add(nodeId);
                    group.RegisterNode(nodeId);
                }
                else if (existingGroupId != groupId)
                {
                    group.RegisterNode(nodeId);
                }

                _groupByNodeIdSlots[nodeId] = groupId;
                _nodeStateSlots[nodeId] = NodeStatePending;
            }
        }
    }

    /// <summary>
    /// Marks a node as skipped so it no longer blocks dispatch of its batching group.
    /// If this was the last remaining node in the group, the group is dispatched.
    /// </summary>
    /// <param name="nodeId">The execution node ID to skip.</param>
    public void SkipNode(int nodeId)
    {
        List<PendingRequest>? pendingRequests = null;
        var needsDispatch = false;

        lock (_sync)
        {
            if (_aborted)
            {
                return;
            }

            if ((uint)nodeId >= (uint)_groupByNodeIdSlots.Length)
            {
                return;
            }

            var groupId = _groupByNodeIdSlots[nodeId];

            if (groupId < 0 || !_groups.TryGetValue(groupId, out var group))
            {
                return;
            }

            group.Skip(nodeId, _nodeStateSlots);

            if (group.TryCreateDispatch(out pendingRequests))
            {
                needsDispatch = true;
                RemoveGroup(group);
            }
        }

        if (needsDispatch)
        {
            BeginDispatchGroup(pendingRequests!);
        }
    }

    /// <summary>
    /// Aborts the dispatcher, failing all pending requests with the given error.
    /// Subsequent calls to <see cref="ExecuteAsync"/>, <see cref="RegisterGroup"/>,
    /// and <see cref="SkipNode"/> become no-ops.
    /// </summary>
    /// <param name="error">
    /// The error to propagate to pending requests. If <c>null</c>, an
    /// <see cref="OperationCanceledException"/> is used.
    /// </param>
    public void Abort(Exception? error = null)
    {
        PendingRequest[] pendingRequests;
        Exception abortError;

        lock (_sync)
        {
            if (_aborted)
            {
                return;
            }

            _aborted = true;
            _abortError = error ?? new OperationCanceledException(SourceSchemaRequestDispatcher_OperationAborted);
            abortError = _abortError;
            pendingRequests = [.. _groups.Values.SelectMany(static t => t.PendingRequests)];

            _groups.Clear();
            ClearNodeIdSlots();
        }

        foreach (var pendingRequest in pendingRequests)
        {
            pendingRequest.Completion.TrySetException(abortError);
        }
    }

    /// <summary>
    /// Resets the dispatcher to its initial state, clearing all groups and the aborted flag.
    /// Any pending requests from a prior event are abandoned (they should have been
    /// completed or aborted before calling this).
    /// </summary>
    public void Reset()
    {
        lock (_sync)
        {
            _aborted = false;
            _abortError = null;
            _groups.Clear();
            ClearNodeIdSlots();
        }
    }

    private void BeginDispatchGroup(List<PendingRequest> pendingRequests)
    {
        // if pending requests is 0 it mean the the whole group was skipped and we do not need to do anything.
        if (pendingRequests.Count == 0)
        {
            return;
        }

        // in all other cases we dispatch the group asynchronously.
        _ = DispatchGroupAsync(pendingRequests);
    }

    private async Task DispatchGroupAsync(List<PendingRequest> pendingRequests)
    {
        try
        {
            if (pendingRequests.Count == 1)
            {
                var pendingRequest = pendingRequests[0];

                var client = _clientScope.GetClient(
                    pendingRequest.Request.SchemaName,
                    pendingRequest.Request.OperationType);

                await DispatchSingleAsync(client, pendingRequest).ConfigureAwait(false);
            }
            else
            {
                var client = _clientScope.GetClient(
                    pendingRequests[0].Request.SchemaName,
                    pendingRequests[0].Request.OperationType);

                await DispatchBatchAsync(client, pendingRequests).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            foreach (var pendingRequest in pendingRequests)
            {
                pendingRequest.Completion.TrySetException(ex);
            }
        }
    }

    private async ValueTask DispatchSingleAsync(
        ISourceSchemaClient client,
        PendingRequest pendingRequest)
    {
        try
        {
            var response = await client.ExecuteAsync(
                    _context,
                    pendingRequest.Request,
                    _requestAborted)
                .ConfigureAwait(false);

            if (!pendingRequest.Completion.TrySetResult(response))
            {
                response.Dispose();
            }
        }
        catch (OperationCanceledException)
        {
            pendingRequest.Completion.TrySetCanceled();
        }
        catch (Exception ex)
        {
            pendingRequest.Completion.TrySetException(ex);
        }
    }

    private async ValueTask DispatchBatchAsync(
        ISourceSchemaClient client,
        List<PendingRequest> pendingRequests)
    {
        try
        {
            var requests = new SourceSchemaClientRequest[pendingRequests.Count];

            for (var i = 0; i < pendingRequests.Count; i++)
            {
                requests[i] = pendingRequests[i].Request;
            }

            var responses = await client.ExecuteBatchAsync(
                    _context,
                    ImmutableCollectionsMarshal.AsImmutableArray(requests),
                    _requestAborted)
                .ConfigureAwait(false);

            if (responses.Length != pendingRequests.Count)
            {
                throw new InvalidOperationException(
                    SourceSchemaRequestDispatcher_BatchResponseCountMismatch);
            }

            for (var i = 0; i < pendingRequests.Count; i++)
            {
                var pendingRequest = pendingRequests[i];
                var response = responses[i];

                if (!pendingRequest.Completion.TrySetResult(response))
                {
                    response.Dispose();
                }
            }
        }
        catch (OperationCanceledException)
        {
            foreach (var pendingRequest in pendingRequests)
            {
                pendingRequest.Completion.TrySetCanceled();
            }
        }
        catch (Exception ex)
        {
            foreach (var pendingRequest in pendingRequests)
            {
                pendingRequest.Completion.TrySetException(ex);
            }
        }
    }

    private Exception CreateAbortException()
        => _abortError ?? new OperationCanceledException(SourceSchemaRequestDispatcher_OperationAborted);

    private void RemoveGroup(GroupState group)
    {
        _groups.Remove(group.Id);

        foreach (var nodeId in group.NodeIds)
        {
            if ((uint)nodeId < (uint)_groupByNodeIdSlots.Length)
            {
                _groupByNodeIdSlots[nodeId] = -1;
                _nodeStateSlots[nodeId] = NodeStateUnregistered;
            }
        }
    }

    private void ClearNodeIdSlots()
    {
        if (_trackedNodeIdSlots.Count == 0)
        {
            return;
        }

        foreach (var nodeId in _trackedNodeIdSlots)
        {
            if ((uint)nodeId < (uint)_groupByNodeIdSlots.Length)
            {
                _groupByNodeIdSlots[nodeId] = -1;
                _nodeStateSlots[nodeId] = NodeStateUnregistered;
            }
        }

        _trackedNodeIdSlots.Clear();
    }

    private void EnsureNodeIdSlotCapacity(int minCapacity)
    {
        if (_groupByNodeIdSlots.Length >= minCapacity)
        {
            return;
        }

        var newCapacity = _groupByNodeIdSlots.Length == 0 ? 8 : _groupByNodeIdSlots.Length;

        while (newCapacity < minCapacity)
        {
            newCapacity *= 2;
        }

        var groupByNodeIdSlots = new int[newCapacity];
        var nodeStateSlots = new int[newCapacity];
        Array.Fill(groupByNodeIdSlots, -1);
        Array.Fill(nodeStateSlots, NodeStateUnregistered);

        if (_groupByNodeIdSlots.Length > 0)
        {
            Array.Copy(_groupByNodeIdSlots, groupByNodeIdSlots, _groupByNodeIdSlots.Length);
            Array.Copy(_nodeStateSlots, nodeStateSlots, _nodeStateSlots.Length);
        }

        _groupByNodeIdSlots = groupByNodeIdSlots;
        _nodeStateSlots = nodeStateSlots;
    }

    private sealed class GroupState(int id, int initialCapacity)
    {
        private readonly List<int> _nodeIds = new(initialCapacity);
        private readonly List<PendingRequest> _pendingRequests = new(initialCapacity);
        private int _remainingNodes;
        private bool _dispatchCreated;

        public int Id { get; } = id;

        public IEnumerable<int> NodeIds => _nodeIds;

        public IReadOnlyList<PendingRequest> PendingRequests => _pendingRequests;

        public void RegisterNode(int nodeId)
        {
            _nodeIds.Add(nodeId);
            _remainingNodes++;
        }

        public bool TrySubmit(
            SourceSchemaClientRequest request,
            int[] nodeStateSlots,
            out PendingRequest? pendingRequest)
        {
            var nodeId = request.Node.Id;
            var nodeState =
                (uint)nodeId < (uint)nodeStateSlots.Length
                    ? nodeStateSlots[nodeId]
                    : NodeStateUnregistered;

            if (nodeState == NodeStateSubmitted)
            {
                throw new InvalidOperationException(
                    string.Format(
                        SourceSchemaRequestDispatcher_DuplicateNodeSubmission,
                        nodeId));
            }

            if (nodeState != NodeStatePending)
            {
                pendingRequest = null;
                return false;
            }

            nodeStateSlots[nodeId] = NodeStateSubmitted;
            _remainingNodes--;

            pendingRequest = new PendingRequest(request);
            _pendingRequests.Add(pendingRequest);

            return true;
        }

        public void Skip(int nodeId, int[] nodeStateSlots)
        {
            if ((uint)nodeId < (uint)nodeStateSlots.Length
                && nodeStateSlots[nodeId] == NodeStatePending)
            {
                nodeStateSlots[nodeId] = NodeStateSkipped;
                _remainingNodes--;
            }
        }

        public bool TryCreateDispatch(out List<PendingRequest>? pendingRequests)
        {
            if (_dispatchCreated || _remainingNodes > 0)
            {
                pendingRequests = null;
                return false;
            }

            _dispatchCreated = true;
            pendingRequests = _pendingRequests;
            return true;
        }
    }

    private sealed class PendingRequest(SourceSchemaClientRequest request)
    {
        public SourceSchemaClientRequest Request { get; } = request;

        public TaskCompletionSource<SourceSchemaClientResponse> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
