using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Processing;

internal sealed partial class DeferExecutionCoordinator
{
    private readonly object _sync = new();
    private readonly Dictionary<DeferredBranchKey, int> _branchIdLookup = [];
    private readonly Dictionary<int, DeferredBranch> _branchLookup = [];
    private HashSet<int>? _mainBranchChildren;
    private readonly Dictionary<int, OperationResult> _completed = [];
    private readonly HashSet<int> _delivered = [];
    private readonly List<OperationResult> _results = [];
    private readonly AsyncAutoResetEvent _signal = new();
    private BranchTracker _branchTracker = null!;
    private int _mainBranchId;
    private ImmutableList<PendingResult>.Builder? _pendingBuilder;
    private ImmutableList<IIncrementalResult>.Builder? _incrementalBuilder;
    private ImmutableList<CompletedResult>.Builder? _completedBuilder;
    private Queue<int>? _processQueue;
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
    /// Gets whether any deferred execution branches have been registered.
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
                _branchLookup.Add(newBranchId, new DeferredBranch(path, deferUsage, currentBranchId));
                _branchIdLookup.Add(key, newBranchId);
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
    /// If the parent branch has already been delivered, the result is composed
    /// and delivered immediately; otherwise it is stored until the parent is delivered.
    /// </summary>
    public void EnqueueResult(OperationResult result, int branchId)
    {
        AssertInitialized();

        lock (_sync)
        {
            _completed[branchId] = result;

            if (IsParentDeliveredUnsafe(branchId)
                && _completed.Remove(branchId, out var readyResult))
            {
                ComposeAndDeliverUnsafe(branchId, readyResult);
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

            lock (_sync)
            {
                snapshot ??= [];
                snapshot.Clear();
                snapshot.AddRange(_results);
                _results.Clear();
            }

            foreach (var result in snapshot)
            {
                yield return result;
            }

            if (_isComplete)
            {
                yield break;
            }
        }
    }

    private void ComposeAndDeliverUnsafe(int branchId, OperationResult result)
    {
        var children = GetChildrenUnsafe(branchId);

        if (children.Count > 0)
        {
            var pendingBuilder = _pendingBuilder ??= ImmutableList.CreateBuilder<PendingResult>();
            var incrementalBuilder = _incrementalBuilder ??= ImmutableList.CreateBuilder<IIncrementalResult>();
            var completedBuilder = _completedBuilder ??= ImmutableList.CreateBuilder<CompletedResult>();
            var processQueue = _processQueue ??= new Queue<int>();

            pendingBuilder.Clear();
            incrementalBuilder.Clear();
            completedBuilder.Clear();
            processQueue.Clear();

            foreach (var childId in children)
            {
                var child = _branchLookup[childId];

                pendingBuilder.Add(
                    new PendingResult(
                        childId,
                        child.Path,
                        child.Group.Label));

                if (_completed.Remove(childId, out var childResult))
                {
                    result.RegisterForCleanup(childResult);
                    AddCompletedBranch(childId, childResult, incrementalBuilder, completedBuilder);
                    _delivered.Add(childId);
                    _pendingBranches--;
                    processQueue.Enqueue(childId);
                }
            }

            while (processQueue.TryDequeue(out var parentId))
            {
                foreach (var grandchildId in GetChildrenUnsafe(parentId))
                {
                    var branch = _branchLookup[grandchildId];

                    pendingBuilder.Add(
                        new PendingResult(
                            grandchildId,
                            branch.Path,
                            branch.Group.Label));

                    if (_completed.Remove(grandchildId, out var gcResult))
                    {
                        result.RegisterForCleanup(gcResult);
                        AddCompletedBranch(grandchildId, gcResult, incrementalBuilder, completedBuilder);
                        _delivered.Add(grandchildId);
                        _pendingBranches--;
                        processQueue.Enqueue(grandchildId);
                    }
                }
            }

            result.Pending = pendingBuilder.ToImmutable();
            result.Incremental = incrementalBuilder.ToImmutable();
            result.Completed = completedBuilder.ToImmutable();
        }

        // For deferred branches (not main branch), transform the result's data into an incremental result.
        // Per spec: only the initial payload has root "data"; subsequent payloads use "incremental" array.
        if (branchId != _mainBranchId)
        {
            var incrementalBuilder = _incrementalBuilder ??= ImmutableList.CreateBuilder<IIncrementalResult>();
            var completedBuilder = _completedBuilder ??= ImmutableList.CreateBuilder<CompletedResult>();

            if (children.Count == 0)
            {
                incrementalBuilder.Clear();
                completedBuilder.Clear();
            }

            AddCompletedBranch(branchId, result, incrementalBuilder, completedBuilder);

            result.Incremental = incrementalBuilder.ToImmutable();
            result.Completed = completedBuilder.ToImmutable();
            result.Data = null;
            result.Errors = [];
        }

        _delivered.Add(branchId);

        if (branchId != _mainBranchId)
        {
            _pendingBranches--;
        }

        var isComplete = _delivered.Contains(_mainBranchId) && _pendingBranches == 0;
        result.HasNext = !isComplete;

        _results.Add(result);
        _isComplete = isComplete;
        _signal.Set();
    }

    /// <summary>
    /// Determines whether the parent of the specified branch has already
    /// delivered its result to the response stream.
    /// </summary>
    private bool IsParentDeliveredUnsafe(int branchId)
        => _branchLookup.TryGetValue(branchId, out var branch)
            && _delivered.Contains(branch.ParentBranchId);

    private static void AddCompletedBranch(
        int branchId,
        OperationResult branchResult,
        ImmutableList<IIncrementalResult>.Builder incrementalBuilder,
        ImmutableList<CompletedResult>.Builder completedBuilder)
    {
        if (branchResult.Data.HasValue && !branchResult.Data.Value.IsValueNull)
        {
            // data is valid (possibly with contained errors) — deliver incremental data
            incrementalBuilder.Add(
                new IncrementalObjectResult(
                    branchId,
                    branchResult.Errors,
                    subPath: null,
                    branchResult.Data));
            completedBuilder.Add(new CompletedResult(branchId));
        }
        else
        {
            // errors bubbled above the incremental result's path — no data to deliver
            completedBuilder.Add(new CompletedResult(branchId, branchResult.Errors));
        }
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

    private struct DeferredBranch(Path path, DeferUsage group, int parentBranchId)
    {
        public Path Path { get; } = path;
        public DeferUsage Group { get; } = group;
        public int ParentBranchId { get; } = parentBranchId;
        public HashSet<int>? Children { get; set; }
    }
}
