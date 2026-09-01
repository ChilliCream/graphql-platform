using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Fetching;
using HotChocolate.Text.Json;

namespace HotChocolate.Execution.Processing;

internal sealed partial class DeferExecutionCoordinator
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly Dictionary<DeferredBranchKey, int> _branchIdLookup = [];
    private readonly Dictionary<StreamBranchKey, int> _streamBranchIdLookup = [];
    private readonly Dictionary<int, DeferredBranch> _branchLookup = [];
    private readonly Dictionary<int, OperationResult> _completedResults = [];
    private readonly HashSet<int> _announced = [];
    private readonly HashSet<int> _completedBranches = [];
    private readonly List<OperationResult> _results = [];
    private readonly AsyncAutoResetEvent _signal = new();
    private Dictionary<OperationResult, TaskCompletionSource<bool>>? _rejectedResultCleanups;
    private HashSet<int>? _mainBranchChildren;
    private BranchTracker _branchTracker = null!;
    private int _mainBranchId;
    private volatile bool _hasBranches;
    private volatile bool _isComplete;
    private int _pendingBranches;

#pragma warning disable IDE0052 // Remove unread private members
    private static int s_nextId;
    private readonly int _id;
#pragma warning restore IDE0052 // Remove unread private members

    public DeferExecutionCoordinator()
    {
        _id = Interlocked.Increment(ref s_nextId);
    }

    /// <summary>
    /// Gets whether any deferred or stream execution branches have been registered.
    /// </summary>
    public bool HasBranches => _hasBranches;

    /// <summary>
    /// Registers a new deferred execution branch for the specified <paramref name="deferUsage"/>
    /// and <paramref name="path"/>, returning a unique branch identifier.
    /// If the branch was already registered, the existing identifier is returned.
    /// </summary>
    public int Branch(int currentBranchId, Path path, DeferUsage deferUsage)
    {
        AssertInitialized();

        var key = new DeferredBranchKey(path, deferUsage, currentBranchId);

        lock (_sync)
        {
            if (!_branchIdLookup.TryGetValue(key, out var newBranchId))
            {
                newBranchId = _branchTracker.CreateNewBranchId();
                GetChildrenUnsafe(currentBranchId).Add(newBranchId);
                _branchLookup.Add(
                    newBranchId,
                    new DeferredBranch(path, deferUsage.Label, currentBranchId, BranchKind.Defer));
                _branchIdLookup.Add(key, newBranchId);
                _hasBranches = true;
                _pendingBranches++;
            }

            return newBranchId;
        }
    }

    /// <summary>
    /// Registers a new stream execution branch for the specified <paramref name="path"/>.
    /// The branch shares the same identifier namespace and parent hierarchy as deferred branches.
    /// </summary>
    public int RegisterStreamBranch(int currentBranchId, Path path, string? label)
    {
        AssertInitialized();

        var key = new StreamBranchKey(path, label, currentBranchId);

        lock (_sync)
        {
            if (!_streamBranchIdLookup.TryGetValue(key, out var newBranchId))
            {
                newBranchId = _branchTracker.CreateNewBranchId();
                GetChildrenUnsafe(currentBranchId).Add(newBranchId);
                _branchLookup.Add(
                    newBranchId,
                    new DeferredBranch(path, label, currentBranchId, BranchKind.Stream));
                _streamBranchIdLookup.Add(key, newBranchId);
                _hasBranches = true;
                _pendingBranches++;
            }

            return newBranchId;
        }
    }

    /// <summary>
    /// Enqueues the initial (non-deferred) result for delivery.
    /// Any already-completed child branches are folded in as incremental data.
    /// </summary>
    public void EnqueueResult(OperationResult result)
    {
        AssertInitialized();

        lock (_sync)
        {
            ComposeAndDeliverUnsafe(_mainBranchId, result);
        }
    }

    /// <summary>
    /// Enqueues a deferred result for the specified branch.
    /// If the branch has already been announced, the result is composed and delivered
    /// immediately; otherwise it is stored until the branch data is revealed. A rejected result
    /// returns an awaitable that completes after its cleanup has finished.
    /// </summary>
    public ValueTask EnqueueResult(OperationResult result, int branchId)
    {
        AssertInitialized();

        TaskCompletionSource<bool>? rejectedResultCleanup = null;

        lock (_sync)
        {
            ref var branch = ref CollectionsMarshal.GetValueRefOrNullRef(_branchLookup, branchId);

            if (Unsafe.IsNullRef(ref branch)
                || branch.Kind != BranchKind.Defer
                || _completedBranches.Contains(branchId))
            {
                if (_rejectedResultCleanups?.TryGetValue(result, out rejectedResultCleanup) is true)
                {
                    return new ValueTask(rejectedResultCleanup.Task);
                }

                rejectedResultCleanup = new(TaskCreationOptions.RunContinuationsAsynchronously);
                (_rejectedResultCleanups ??= []).Add(result, rejectedResultCleanup);
            }
            else
            {
                if (_completedResults.TryGetValue(branchId, out var previousResult))
                {
                    result.RegisterForCleanup(previousResult);
                }

                _completedResults[branchId] = result;

                if (_announced.Contains(branchId))
                {
                    if (_completedResults.Remove(branchId, out var readyResult))
                    {
                        ComposeAndDeliverUnsafe(branchId, readyResult);
                    }
                }
            }
        }

        if (rejectedResultCleanup is not null)
        {
            StartRejectedResultCleanup(result, rejectedResultCleanup);
            return new ValueTask(rejectedResultCleanup.Task);
        }

        return ValueTask.CompletedTask;
    }

    private static void StartRejectedResultCleanup(
        OperationResult result,
        TaskCompletionSource<bool> completion)
    {
        try
        {
            var cleanup = result.DisposeAsync();

            if (cleanup.IsCompletedSuccessfully)
            {
                completion.SetResult(true);
            }
            else
            {
                _ = CompleteRejectedResultCleanupAsync(cleanup, completion);
            }
        }
        catch (Exception exception)
        {
            completion.SetException(exception);
        }
    }

    private static async Task CompleteRejectedResultCleanupAsync(
        ValueTask cleanup,
        TaskCompletionSource<bool> completion)
    {
        try
        {
            await cleanup.ConfigureAwait(false);
            completion.SetResult(true);
        }
        catch (Exception exception)
        {
            completion.SetException(exception);
        }
    }

    /// <summary>
    /// Enqueues a single streamed list item for the specified branch.
    /// The branch remains pending until <see cref="CompleteStream"/> is called.
    /// </summary>
    public ValueTask EnqueueStreamItem(OperationResult result, int branchId)
    {
        AssertInitialized();

        lock (_sync)
        {
            ref var branch = ref CollectionsMarshal.GetValueRefOrNullRef(_branchLookup, branchId);

            if (Unsafe.IsNullRef(ref branch)
                || branch.Kind != BranchKind.Stream
                || _completedBranches.Contains(branchId))
            {
                return result.DisposeAsync();
            }

            if (!_announced.Contains(branchId))
            {
                (branch.Results ??= []).Add(result);
                return ValueTask.CompletedTask;
            }

            var payload = GetPayloadUnsafe(result, out var isNewPayload);
            if (AddStreamItemUnsafe(branchId, result, payload, isNewPayload))
            {
                AnnounceChildrenUnsafe(branchId, payload);
            }

            CommitPayloadUnsafe(payload, isNewPayload, isPayloadIncremental: isNewPayload);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Completes a stream branch, optionally with errors that prevented normal completion.
    /// </summary>
    public void CompleteStream(int branchId, IReadOnlyList<IError>? errors = null)
    {
        AssertInitialized();

        lock (_sync)
        {
            ref var branch = ref CollectionsMarshal.GetValueRefOrNullRef(_branchLookup, branchId);

            if (Unsafe.IsNullRef(ref branch)
                || branch.Kind != BranchKind.Stream
                || _completedBranches.Contains(branchId))
            {
                return;
            }

            branch.IsStreamComplete = true;
            branch.CompletionErrors = errors;

            if (!_announced.Contains(branchId))
            {
                return;
            }

            var payload = GetPayloadUnsafe(null, out var isNewPayload);
            payload.Completed = payload.Completed.Add(new CompletedResult(branchId, errors));
            DropUnannouncedChildrenUnsafe(branchId, payload);
            CompleteBranchUnsafe(branchId);
            CommitPayloadUnsafe(payload, isNewPayload, isPayloadIncremental: isNewPayload);
        }
    }

    /// <summary>
    /// Aborts all pending branches at or below <paramref name="path"/>.
    /// Announced branches receive a failed completion while branches that were not announced are dropped.
    /// </summary>
    public async ValueTask AbortBranchesAsync(Path path, IReadOnlyList<IError> errors)
    {
        AssertInitialized();
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(errors);

        List<OperationResult>? cleanup = null;

        lock (_sync)
        {
            OperationResult? payload = null;
            var isNewPayload = false;

            foreach (var branchId in _branchLookup.Keys)
            {
                ref var branch = ref CollectionsMarshal.GetValueRefOrNullRef(_branchLookup, branchId);

                if (_completedBranches.Contains(branchId) || !IsAtOrBelow(path, branch.Path))
                {
                    continue;
                }

                DetachBranchResultsUnsafe(branchId, ref cleanup);

                if (_announced.Contains(branchId))
                {
                    payload ??= GetPayloadUnsafe(null, out isNewPayload);
                    payload.Completed = payload.Completed.Add(new CompletedResult(branchId, errors));
                }

                CompleteBranchUnsafe(branchId);
            }

            if (payload is not null)
            {
                RegisterCleanupUnsafe(payload, cleanup);
                cleanup = null;
                CommitPayloadUnsafe(payload, isNewPayload, isPayloadIncremental: isNewPayload);
            }
            else if (_announced.Contains(_mainBranchId) && _pendingBranches == 0)
            {
                payload = GetPayloadUnsafe(null, out isNewPayload);
                RegisterCleanupUnsafe(payload, cleanup);
                cleanup = null;
                CommitPayloadUnsafe(payload, isNewPayload, isPayloadIncremental: isNewPayload);
            }
        }

        if (cleanup is not null)
        {
            foreach (var result in cleanup)
            {
                await result.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Returns an async stream of composed operation results in delivery order.
    /// The stream completes automatically when all branches have been delivered.
    /// </summary>
    public async IAsyncEnumerable<OperationResult> ReadResultsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AssertInitialized();

        List<OperationResult>? snapshot = null;
        await using var registration = cancellationToken.Register(_signal.Set);

        while (!cancellationToken.IsCancellationRequested)
        {
            await _signal;

            cancellationToken.ThrowIfCancellationRequested();

            // Read both the results and the completion flag inside the same lock
            // to avoid a race where a final delivery sets _isComplete between
            // the snapshot read and the completion check.
            bool isComplete;

            lock (_sync)
            {
                snapshot ??= [];
                snapshot.Clear();
                snapshot.AddRange(_results);
                _results.Clear();
                isComplete = _isComplete;
            }

            foreach (var result in snapshot)
            {
                yield return result;
            }

            if (isComplete)
            {
                yield break;
            }
        }
    }

    private void ComposeAndDeliverUnsafe(int branchId, OperationResult result)
    {
        var isInitialPayload = branchId == _mainBranchId;
        OperationResult payload;
        bool isNewPayload;

        if (isInitialPayload)
        {
            payload = result;
            isNewPayload = true;
        }
        else
        {
            payload = GetPayloadUnsafe(result, out isNewPayload);
        }

        if (!ReferenceEquals(payload, result))
        {
            payload.RegisterForCleanup(result);
        }

        _announced.Add(branchId);

        var hasData = result.Data is { IsValueNull: false };

        if (isInitialPayload || hasData)
        {
            AnnounceChildrenUnsafe(branchId, payload);
        }

        if (!isInitialPayload)
        {
            AddCompletedBranch(branchId, result, payload);
            CompleteBranchUnsafe(branchId);

            if (!hasData)
            {
                DropUnannouncedChildrenUnsafe(branchId, payload);
            }
        }

        CommitPayloadUnsafe(payload, isNewPayload, isPayloadIncremental: !isInitialPayload && isNewPayload);
    }

    private void AnnounceChildrenUnsafe(int branchId, OperationResult result)
    {
        var children = GetChildrenUnsafe(branchId);

        if (children.Count == 0)
        {
            return;
        }

        foreach (var childId in children)
        {
            if (!_announced.Add(childId))
            {
                continue;
            }

            ref var child = ref CollectionsMarshal.GetValueRefOrNullRef(_branchLookup, childId);
            result.Pending = result.Pending.Add(new PendingResult(childId, child.Path, child.Label));

            if (child.Kind == BranchKind.Defer
                && _completedResults.Remove(childId, out var childResult))
            {
                result.RegisterForCleanup(childResult);
                AddCompletedBranch(childId, childResult, result);
                CompleteBranchUnsafe(childId);

                if (childResult.Data is { IsValueNull: false })
                {
                    AnnounceChildrenUnsafe(childId, result);
                }
                else
                {
                    DropUnannouncedChildrenUnsafe(childId, result);
                }
            }
            else if (child.Kind == BranchKind.Stream
                && child.Results is { Count: > 0 } results)
            {
                foreach (var streamResult in results)
                {
                    if (AddStreamItemUnsafe(childId, streamResult, result, resultIsStreamResult: false))
                    {
                        AnnounceChildrenUnsafe(childId, result);
                    }
                }

                results.Clear();
            }

            if (child.Kind == BranchKind.Stream && child.IsStreamComplete)
            {
                result.Completed = result.Completed.Add(
                    new CompletedResult(childId, child.CompletionErrors));
                DropUnannouncedChildrenUnsafe(childId, result);
                CompleteBranchUnsafe(childId);
            }
        }
    }

    private static void AddCompletedBranch(
        int branchId,
        OperationResult branchResult,
        OperationResult result)
    {
        if (branchResult.Data.HasValue && !branchResult.Data.Value.IsValueNull)
        {
            // data is valid (possibly with contained errors) — deliver incremental data
            result.Incremental = result.Incremental.Add(
                new IncrementalObjectResult(
                    branchId,
                    branchResult.Errors,
                    subPath: null,
                    branchResult.Data));
            result.Completed = result.Completed.Add(new CompletedResult(branchId));
        }
        else
        {
            // errors bubbled above the incremental result path, so no data can be delivered
            result.Completed = result.Completed.Add(new CompletedResult(branchId, branchResult.Errors));
        }
    }

    private bool AddStreamItemUnsafe(
        int branchId,
        OperationResult streamResult,
        OperationResult result,
        bool resultIsStreamResult)
    {
        if (!streamResult.Data.HasValue)
        {
            if (!resultIsStreamResult)
            {
                result.RegisterForCleanup(streamResult);
            }

            return false;
        }

        if (!resultIsStreamResult)
        {
            result.RegisterForCleanup(streamResult);
        }

        result.Incremental = result.Incremental.Add(
            new IncrementalListResult(branchId, streamResult.Data.Value, streamResult.Errors));
        return true;
    }

    private void DropUnannouncedChildrenUnsafe(int branchId, OperationResult payload)
    {
        foreach (var childId in GetChildrenUnsafe(branchId))
        {
            if (_announced.Contains(childId) || _completedBranches.Contains(childId))
            {
                continue;
            }

            DropUnannouncedChildrenUnsafe(childId, payload);
            DetachBranchResultsUnsafe(childId, payload);
            CompleteBranchUnsafe(childId);
        }
    }

    private void DetachBranchResultsUnsafe(int branchId, OperationResult payload)
    {
        if (_completedResults.Remove(branchId, out var completedResult))
        {
            payload.RegisterForCleanup(completedResult);
        }

        ref var branch = ref CollectionsMarshal.GetValueRefOrNullRef(_branchLookup, branchId);

        if (branch.Results is { Count: > 0 } results)
        {
            foreach (var result in results)
            {
                payload.RegisterForCleanup(result);
            }

            results.Clear();
        }
    }

    private void DetachBranchResultsUnsafe(int branchId, ref List<OperationResult>? cleanup)
    {
        if (_completedResults.Remove(branchId, out var completedResult))
        {
            (cleanup ??= []).Add(completedResult);
        }

        ref var branch = ref CollectionsMarshal.GetValueRefOrNullRef(_branchLookup, branchId);

        if (branch.Results is { Count: > 0 } results)
        {
            cleanup ??= [];
            cleanup.AddRange(results);
            results.Clear();
        }
    }

    private static void RegisterCleanupUnsafe(OperationResult payload, List<OperationResult>? cleanup)
    {
        if (cleanup is not null)
        {
            foreach (var result in cleanup)
            {
                payload.RegisterForCleanup(result);
            }
        }
    }

    private OperationResult GetPayloadUnsafe(OperationResult? streamResult, out bool isNewPayload)
    {
        if (_results.Count > 0 && _results[^1].Data is null)
        {
            isNewPayload = false;
            return _results[^1];
        }

        isNewPayload = true;
        return streamResult
            ?? new OperationResult(new OperationResultData(s_emptyData, false, EmptyFormatter.Instance, null));
    }

    private void CommitPayloadUnsafe(OperationResult result, bool isNewPayload, bool isPayloadIncremental)
    {
        var isComplete = _announced.Contains(_mainBranchId) && _pendingBranches == 0;
        result.HasNext = !isComplete;

        if (isPayloadIncremental && result.Data.HasValue)
        {
            result.Data = null;
            result.Errors = [];
        }

        if (isNewPayload)
        {
            _results.Add(result);
        }

        _isComplete = isComplete;
        _signal.Set();
    }

    private void CompleteBranchUnsafe(int branchId)
    {
        if (_completedBranches.Add(branchId))
        {
            _pendingBranches--;
        }
    }

    private static bool IsAtOrBelow(Path ancestor, Path path)
    {
        if (path.Length < ancestor.Length)
        {
            return false;
        }

        while (path.Length > ancestor.Length)
        {
            path = path.Parent;
        }

        return path.Equals(ancestor);
    }

    /// <summary>
    /// Gets the child branches for the specified branch.
    /// For the main branch, uses the dedicated field; for deferred branches,
    /// uses the children set stored in the branch lookup.
    /// </summary>
    private HashSet<int> GetChildrenUnsafe(int branchId)
    {
        if (branchId == _mainBranchId)
        {
            return _mainBranchChildren ??= [];
        }

        ref var branch = ref CollectionsMarshal.GetValueRefOrNullRef(_branchLookup, branchId);

        if (Unsafe.IsNullRef(ref branch))
        {
            return [];
        }

        return branch.Children ??= [];
    }

    private readonly record struct DeferredBranchKey(Path Path, DeferUsage Group, int ParentBranchId);

    private readonly record struct StreamBranchKey(Path Path, string? Label, int ParentBranchId);

    private struct DeferredBranch(Path path, string? label, int parentBranchId, BranchKind kind)
    {
        public Path Path { get; } = path;
        public string? Label { get; } = label;
        public int ParentBranchId { get; } = parentBranchId;
        public BranchKind Kind { get; } = kind;
        public HashSet<int>? Children { get; set; }
        public List<OperationResult>? Results { get; set; }
        public IReadOnlyList<IError>? CompletionErrors { get; set; }
        public bool IsStreamComplete { get; set; }
    }

    private enum BranchKind : byte
    {
        Defer,
        Stream
    }

    private static readonly object s_emptyData = new();

    private sealed class EmptyFormatter : IRawJsonFormatter
    {
        public static EmptyFormatter Instance { get; } = new();

        public void WriteDataTo(JsonWriter jsonWriter)
        {
        }
    }
}
