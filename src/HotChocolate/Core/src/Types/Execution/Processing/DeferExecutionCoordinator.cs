using System.Collections.Immutable;
using System.Threading.Channels;

namespace HotChocolate.Execution.Processing;

internal sealed partial class DeferExecutionCoordinator
{
    private readonly object _sync = new();
    private readonly Dictionary<DeferredBranchInfo, int> _branchIdLookup = [];
    private readonly Dictionary<int, DeferredBranchInfo> _branchInfoLookup = [];
    private readonly Dictionary<int, HashSet<int>> _branches = [];
    private readonly Dictionary<int, OperationResult> _completed = [];
    private readonly HashSet<int> _delivered = [];
    private BranchTracker _branchTracker = null!;
    private int _mainBranchId;
    private ImmutableList<PendingResult>.Builder? _pendingBuilder;
    private ImmutableList<IIncrementalResult>.Builder? _incrementalBuilder;
    private ImmutableList<CompletedResult>.Builder? _completedBuilder;
    private Queue<int>? _processQueue;
    private Channel<OperationResult> _resultChannel = null!;
    private volatile bool _hasBranches;
    private int _pendingBranches;

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
        var branchInfo = new DeferredBranchInfo(path, deferUsage, currentBranchId);

        lock (_sync)
        {
            if (!_branchIdLookup.TryGetValue(branchInfo, out var newBranchId))
            {
                newBranchId = _branchTracker.CreateNewBranchId();
                GetBranchesUnsafe(currentBranchId).Add(newBranchId);
                _branchInfoLookup.Add(newBranchId, branchInfo);
                _branchIdLookup.Add(branchInfo, newBranchId);
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
    public IAsyncEnumerable<OperationResult> ReadResultsAsync(
        CancellationToken cancellationToken = default)
        => _resultChannel.Reader.ReadAllAsync(cancellationToken);

    private void ComposeAndDeliverUnsafe(int branchId, OperationResult result)
    {
        var childBranches = GetBranchesUnsafe(branchId);

        if (childBranches.Count > 0)
        {
            var pendingBuilder = _pendingBuilder ??= ImmutableList.CreateBuilder<PendingResult>();
            var incrementalBuilder = _incrementalBuilder ??= ImmutableList.CreateBuilder<IIncrementalResult>();
            var completedBuilder = _completedBuilder ??= ImmutableList.CreateBuilder<CompletedResult>();
            var processQueue = _processQueue ??= new Queue<int>();

            pendingBuilder.Clear();
            incrementalBuilder.Clear();
            completedBuilder.Clear();
            processQueue.Clear();

            foreach (var childId in childBranches)
            {
                var childInfo = _branchInfoLookup[childId];

                pendingBuilder.Add(
                    new PendingResult(
                        childId,
                        childInfo.Path,
                        childInfo.Group.Label));

                if (_completed.Remove(childId, out var childResult))
                {
                    incrementalBuilder.Add(
                        new IncrementalObjectResult(
                            childId,
                            childResult.Errors,
                            subPath: null,
                            childResult.Data));

                    completedBuilder.Add(new CompletedResult(childId));
                    _delivered.Add(childId);
                    _pendingBranches--;
                    processQueue.Enqueue(childId);
                }
            }

            while (processQueue.TryDequeue(out var parentId))
            {
                foreach (var grandchildId in GetBranchesUnsafe(parentId))
                {
                    var info = _branchInfoLookup[grandchildId];

                    pendingBuilder.Add(
                        new PendingResult(
                            grandchildId,
                            info.Path,
                            info.Group.Label));

                    if (_completed.Remove(grandchildId, out var gcResult))
                    {
                        incrementalBuilder.Add(
                            new IncrementalObjectResult(
                                grandchildId,
                                gcResult.Errors,
                                subPath: null,
                                gcResult.Data));

                        completedBuilder.Add(new CompletedResult(grandchildId));
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

        _delivered.Add(branchId);

        if (branchId != _mainBranchId)
        {
            _pendingBranches--;
        }

        _resultChannel.Writer.TryWrite(result);

        if (_delivered.Contains(_mainBranchId) && _pendingBranches == 0)
        {
            _resultChannel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Determines whether the parent of the specified branch has already
    /// delivered its result to the response stream.
    /// </summary>
    private bool IsParentDeliveredUnsafe(int branchId)
        => _branchInfoLookup.TryGetValue(branchId, out var info)
            && _delivered.Contains(info.ParentBranchId);

    /// <summary>
    /// Gets the child branches that were created from the execution branch
    /// represented by the specified <paramref name="branchId"/>.
    /// </summary>
    private HashSet<int> GetBranchesUnsafe(int branchId)
    {
        if (!_branches.TryGetValue(branchId, out var branches))
        {
            branches = [];
            _branches.Add(branchId, branches);
        }

        return branches;
    }

    private readonly record struct DeferredBranchInfo(Path Path, DeferUsage Group, int ParentBranchId);
}
