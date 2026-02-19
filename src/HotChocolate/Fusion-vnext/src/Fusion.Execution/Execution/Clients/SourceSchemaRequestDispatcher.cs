using HotChocolate.Language;

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
internal sealed class SourceSchemaRequestDispatcher(
    Func<OperationPlanContext, string, OperationType, ISourceSchemaClient> clientResolver)
    : ISourceSchemaScheduler
    , ISourceSchemaDispatcher
{
    private readonly object _sync = new();
    private readonly Dictionary<int, GroupState> _groups = [];
    private readonly Dictionary<int, int> _groupByNodeId = [];
    private Exception? _abortError;
    private bool _aborted;

    public ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.BatchingGroupId is not int groupId
            || request.OperationType is OperationType.Subscription)
        {
            var client = clientResolver(context, request.SchemaName, request.OperationType);
            return client.ExecuteAsync(context, request, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        PendingRequest? pendingRequest = null;
        GroupDispatch? dispatch = null;
        bool useGrouping;
        Exception? abortError = null;

        lock (_sync)
        {
            if (_aborted)
            {
                abortError = CreateAbortException();
                useGrouping = false;
            }
            else if (_groups.TryGetValue(groupId, out var group)
                && group.TrySubmit(context, cancellationToken, request, out pendingRequest))
            {
                useGrouping = true;

                if (group.TryCreateDispatch(out dispatch))
                {
                    RemoveGroup(group);
                }
            }
            else
            {
                useGrouping = false;
            }
        }

        if (abortError is not null)
        {
            return ValueTask.FromException<SourceSchemaClientResponse>(abortError);
        }

        if (!useGrouping)
        {
            var client = clientResolver(context, request.SchemaName, request.OperationType);
            return client.ExecuteAsync(context, request, cancellationToken);
        }

        if (dispatch is not null)
        {
            _ = DispatchGroupAsync(dispatch);
        }

        return new ValueTask<SourceSchemaClientResponse>(pendingRequest!.Completion.Task);
    }

    public void RegisterGroup(int groupId, IReadOnlyList<int> nodeIds)
    {
        ArgumentNullException.ThrowIfNull(nodeIds);

        if (nodeIds.Count == 0)
        {
            return;
        }

        GroupDispatch? dispatch = null;

        lock (_sync)
        {
            if (_aborted)
            {
                return;
            }

            if (!_groups.TryGetValue(groupId, out var group))
            {
                group = new GroupState(groupId);
                _groups.Add(groupId, group);
            }

            group.Register(nodeIds);

            foreach (var nodeId in nodeIds)
            {
                _groupByNodeId[nodeId] = groupId;
            }

            if (group.TryCreateDispatch(out dispatch))
            {
                RemoveGroup(group);
            }
        }

        if (dispatch is not null)
        {
            _ = DispatchGroupAsync(dispatch);
        }
    }

    public void SkipNode(int nodeId)
    {
        GroupDispatch? dispatch = null;

        lock (_sync)
        {
            if (_aborted)
            {
                return;
            }

            if (!_groupByNodeId.TryGetValue(nodeId, out var groupId)
                || !_groups.TryGetValue(groupId, out var group))
            {
                return;
            }

            group.Skip(nodeId);

            if (group.TryCreateDispatch(out dispatch))
            {
                RemoveGroup(group);
            }
        }

        if (dispatch is not null)
        {
            _ = DispatchGroupAsync(dispatch);
        }
    }

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
            _abortError = error ?? new OperationCanceledException("The operation execution was aborted.");
            abortError = _abortError;
            pendingRequests = [.. _groups.Values.SelectMany(static t => t.PendingRequests)];

            _groups.Clear();
            _groupByNodeId.Clear();
        }

        foreach (var pendingRequest in pendingRequests)
        {
            pendingRequest.Completion.TrySetException(abortError);
        }
    }

    private async Task DispatchGroupAsync(GroupDispatch dispatch)
    {
        try
        {
            var partitions = new Dictionary<(string SchemaName, OperationType OperationType), List<PendingRequest>>();

            foreach (var pendingRequest in dispatch.PendingRequests)
            {
                var key = (pendingRequest.Request.SchemaName, pendingRequest.Request.OperationType);

                if (!partitions.TryGetValue(key, out var partition))
                {
                    partition = [];
                    partitions.Add(key, partition);
                }

                partition.Add(pendingRequest);
            }

            foreach (var partition in partitions.Values)
            {
                var first = partition[0];
                var client = clientResolver(
                    dispatch.Context,
                    first.Request.SchemaName,
                    first.Request.OperationType);

                if (partition.Count == 1)
                {
                    await DispatchSingleAsync(client, dispatch.Context, partition[0], dispatch.CancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                await DispatchBatchAsync(client, dispatch.Context, partition, dispatch.CancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            foreach (var pendingRequest in dispatch.PendingRequests)
            {
                pendingRequest.Completion.TrySetException(ex);
            }
        }
    }

    private static async ValueTask DispatchSingleAsync(
        ISourceSchemaClient client,
        OperationPlanContext context,
        PendingRequest pendingRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.ExecuteAsync(
                    context,
                    pendingRequest.Request,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!pendingRequest.Completion.TrySetResult(response))
            {
                response.Dispose();
            }
        }
        catch (Exception ex)
        {
            pendingRequest.Completion.TrySetException(ex);
        }
    }

    private static async ValueTask DispatchBatchAsync(
        ISourceSchemaClient client,
        OperationPlanContext context,
        List<PendingRequest> partition,
        CancellationToken cancellationToken)
    {
        var requests = new List<SourceSchemaClientRequest>(partition.Count);
        var expectedNodeIds = new HashSet<int>(partition.Count);

        foreach (var pendingRequest in partition)
        {
            requests.Add(pendingRequest.Request);
            expectedNodeIds.Add(pendingRequest.Request.Node.Id);
        }

        IReadOnlyDictionary<int, SourceSchemaClientResponse> responses;

        try
        {
            responses = await client.ExecuteBatchAsync(context, requests, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            foreach (var pendingRequest in partition)
            {
                pendingRequest.Completion.TrySetException(ex);
            }

            return;
        }

        foreach (var pendingRequest in partition)
        {
            var nodeId = pendingRequest.Request.Node.Id;

            if (responses.TryGetValue(nodeId, out var response))
            {
                if (!pendingRequest.Completion.TrySetResult(response))
                {
                    response.Dispose();
                }
            }
            else
            {
                pendingRequest.Completion.TrySetException(
                    new InvalidOperationException(
                        $"The batch response does not contain a result for node '{nodeId}'."));
            }
        }

        foreach (var response in responses)
        {
            if (!expectedNodeIds.Contains(response.Key))
            {
                response.Value.Dispose();
            }
        }
    }

    private Exception CreateAbortException()
        => _abortError ?? new OperationCanceledException("The operation execution was aborted.");

    private void RemoveGroup(GroupState group)
    {
        _groups.Remove(group.Id);

        foreach (var nodeId in group.NodeIds)
        {
            _groupByNodeId.Remove(nodeId);
        }
    }

    private sealed class GroupState(int id)
    {
        private readonly HashSet<int> _nodeIds = [];
        private readonly HashSet<int> _remainingNodeIds = [];
        private readonly Dictionary<int, PendingRequest> _pendingRequests = [];
        private OperationPlanContext? _context;
        private CancellationToken _cancellationToken;
        private bool _dispatchCreated;

        public int Id { get; } = id;

        public IEnumerable<int> NodeIds => _nodeIds;

        public IEnumerable<PendingRequest> PendingRequests => _pendingRequests.Values;

        public void Register(IReadOnlyList<int> nodeIds)
        {
            foreach (var nodeId in nodeIds)
            {
                _nodeIds.Add(nodeId);
                _remainingNodeIds.Add(nodeId);
            }
        }

        public bool TrySubmit(
            OperationPlanContext context,
            CancellationToken cancellationToken,
            SourceSchemaClientRequest request,
            out PendingRequest? pendingRequest)
        {
            var nodeId = request.Node.Id;

            if (!_nodeIds.Contains(nodeId))
            {
                pendingRequest = null;
                return false;
            }

            if (_pendingRequests.TryGetValue(nodeId, out pendingRequest))
            {
                return true;
            }

            _remainingNodeIds.Remove(nodeId);

            _context ??= context;
            _cancellationToken = cancellationToken;

            pendingRequest = new PendingRequest(request);
            _pendingRequests.Add(nodeId, pendingRequest);

            return true;
        }

        public void Skip(int nodeId)
            => _remainingNodeIds.Remove(nodeId);

        public bool TryCreateDispatch(out GroupDispatch? dispatch)
        {
            if (_dispatchCreated || _remainingNodeIds.Count > 0)
            {
                dispatch = null;
                return false;
            }

            _dispatchCreated = true;

            if (_pendingRequests.Count == 0)
            {
                dispatch = null;
                return true;
            }

            dispatch = new GroupDispatch(_context!, _cancellationToken, [.. _pendingRequests.Values]);
            return true;
        }
    }

    private sealed class PendingRequest(SourceSchemaClientRequest request)
    {
        public SourceSchemaClientRequest Request { get; } = request;

        public TaskCompletionSource<SourceSchemaClientResponse> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed record GroupDispatch(
        OperationPlanContext Context,
        CancellationToken CancellationToken,
        IReadOnlyList<PendingRequest> PendingRequests);
}
