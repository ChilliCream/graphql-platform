using System.Threading.Channels;

namespace HotChocolate.Execution.Processing;

internal sealed partial class DeferExecutionCoordinator
{
    /// <summary>
    /// Initializes the coordinator for a new execution cycle.
    /// Must be called before any other operations when leased from a pool.
    /// </summary>
    public void Initialize(BranchTracker branchTracker, int mainBranchId)
    {
        _branchTracker = branchTracker;
        _mainBranchId = mainBranchId;
        _resultChannel = Channel.CreateUnbounded<OperationResult>(
            new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = true
            });
    }

    /// <summary>
    /// Resets the coordinator to its initial state so it can be reused.
    /// </summary>
    public void Reset()
    {
        _branchIdLookup.Clear();
        _branchInfoLookup.Clear();
        _branches.Clear();
        _completed.Clear();
        _delivered.Clear();
        _branchTracker = null!;
        _resultChannel = null!;
        _pendingBuilder = null;
        _incrementalBuilder = null;
        _completedBuilder = null;
        _processQueue = null;
        _hasBranches = false;
        _mainBranchId = 0;
        _pendingBranches = 0;
    }
}
