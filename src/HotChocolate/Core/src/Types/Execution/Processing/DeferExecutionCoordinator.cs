using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Channels;

namespace HotChocolate.Execution.Processing;

internal sealed class DeferExecutionCoordinator
{
    public const int MainBranchId = -1;
    private readonly object _sync = new();
    private readonly ConcurrentDictionary<DeferredBranchInfo, int> _branchIdLookup = new();
    private readonly ConcurrentDictionary<int, DeferredBranchInfo> _branchInfoLookup = new();
    private readonly ConcurrentDictionary<int, HashSet<int>> _branches = new();
    private readonly ConcurrentDictionary<int, OperationResult> _completed = new();
    private readonly HashSet<int> _delivered = [];
    private readonly Channel<OperationResult> _resultSignal = Channel.CreateUnbounded<OperationResult>();
    private int _nextId;

    public int Branch(int currentBranchId, Path path, DeferUsage deferUsage)
    {
        var resultInfo = new DeferredBranchInfo(path, deferUsage);

        if (!_branchIdLookup.TryGetValue(resultInfo, out var newBranchId))
        {
            lock (_sync)
            {
                if (!_branchIdLookup.TryGetValue(resultInfo, out newBranchId))
                {
                    newBranchId = _nextId++;
                    GetBranchesUnsafe(currentBranchId).Add(newBranchId);
                    _branchInfoLookup.TryAdd(newBranchId, resultInfo);
                    _branchIdLookup.TryAdd(resultInfo, newBranchId);
                }
            }
        }

        return newBranchId;
    }

    public void EnqueueResult(OperationResult result)
    {
        lock (_sync)
        {
            ComposeAndDeliver(MainBranchId, result);
        }
    }

    public void EnqueueResult(OperationResult result, int branchId)
    {
        lock (_sync)
        {
            _completed.TryAdd(branchId, result);

            if (IsParentDelivered(branchId)
                && _completed.TryRemove(branchId, out var readyResult))
            {
                ComposeAndDeliver(branchId, readyResult);
            }
        }
    }

    public void Complete()
    {
        _resultSignal.Writer.TryComplete();
    }

    public IAsyncEnumerable<OperationResult> ReadResultsAsync(
        CancellationToken cancellationToken = default)
        => _resultSignal.Reader.ReadAllAsync(cancellationToken);

    private void ComposeAndDeliver(int branchId, OperationResult result)
    {
        var childBranches = GetBranchesUnsafe(branchId);

        if (childBranches.Count > 0)
        {
            var pendingBuilder = ImmutableList.CreateBuilder<PendingResult>();
            var incrementalBuilder = ImmutableList.CreateBuilder<IIncrementalResult>();
            var completedBuilder = ImmutableList.CreateBuilder<CompletedResult>();
            var toProcess = new Queue<int>();

            foreach (var childId in childBranches)
            {
                var childInfo = _branchInfoLookup[childId];

                pendingBuilder.Add(
                    new PendingResult(childId, childInfo.Path, childInfo.Group.Label));

                if (_completed.TryRemove(childId, out var childResult))
                {
                    incrementalBuilder.Add(
                        new IncrementalObjectResult(
                            childId,
                            childResult.Errors,
                            subPath: null,
                            childResult.Data));

                    completedBuilder.Add(new CompletedResult(childId));
                    _delivered.Add(childId);
                    toProcess.Enqueue(childId);
                }
            }

            while (toProcess.TryDequeue(out var parentId))
            {
                foreach (var grandchildId in GetBranchesUnsafe(parentId))
                {
                    var info = _branchInfoLookup[grandchildId];

                    pendingBuilder.Add(
                        new PendingResult(grandchildId, info.Path, info.Group.Label));

                    if (_completed.TryRemove(grandchildId, out var gcResult))
                    {
                        incrementalBuilder.Add(
                            new IncrementalObjectResult(
                                grandchildId,
                                gcResult.Errors,
                                subPath: null,
                                gcResult.Data));

                        completedBuilder.Add(new CompletedResult(grandchildId));
                        _delivered.Add(grandchildId);
                        toProcess.Enqueue(grandchildId);
                    }
                }
            }

            result.Pending = pendingBuilder.ToImmutable();
            result.Incremental = incrementalBuilder.ToImmutable();
            result.Completed = completedBuilder.ToImmutable();
        }

        _delivered.Add(branchId);
        _resultSignal.Writer.TryWrite(result);
    }

    private bool IsParentDelivered(int branchId)
    {
        foreach (var (parentId, children) in _branches)
        {
            if (children.Contains(branchId))
            {
                return _delivered.Contains(parentId);
            }
        }

        return false;
    }

    private HashSet<int> GetBranchesUnsafe(int resultId)
    {
        if (!_branches.TryGetValue(resultId, out var branches))
        {
            branches = [];
            _branches.TryAdd(resultId, branches);
        }

        return branches;
    }

    private readonly record struct DeferredBranchInfo(Path Path, DeferUsage Group);
}
