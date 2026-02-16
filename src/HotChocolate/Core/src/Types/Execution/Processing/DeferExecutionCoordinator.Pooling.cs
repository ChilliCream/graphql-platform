using System.Diagnostics;

namespace HotChocolate.Execution.Processing;

internal sealed partial class DeferExecutionCoordinator
{
#if DEBUG
    private bool _isInitialized;
#endif

    [Conditional("DEBUG")]
    private void AssertInitialized()
    {
#if DEBUG
        Debug.Assert(_isInitialized);
#endif
    }

    /// <summary>
    /// Initializes the coordinator for a new execution cycle.
    /// Must be called before any other operations when leased from a pool.
    /// </summary>
    public void Initialize(BranchTracker branchTracker, int mainBranchId)
    {
        Debug.Assert(branchTracker is not null);
        Debug.Assert(mainBranchId > 0);

        _branchTracker = branchTracker;
        _mainBranchId = mainBranchId;

#if DEBUG
        _isInitialized = true;
#endif
    }

    /// <summary>
    /// Resets the coordinator to its initial state so it can be reused.
    /// </summary>
    public void Reset()
    {
        _branchIdLookup.Clear();
        _branchLookup.Clear();
        _mainBranchChildren?.Clear();
        _completed.Clear();
        _delivered.Clear();
        _results.Clear();
        _branchTracker = null!;
        _pendingBuilder = null;
        _incrementalBuilder = null;
        _completedBuilder = null;
        _processQueue = null;
        _hasBranches = false;
        _isComplete = false;
        _mainBranchId = 0;
        _pendingBranches = 0;

#if DEBUG
        _isInitialized = false;
#endif

        if (_results.Capacity > 64)
        {
            _results.Capacity = 64;
        }
    }
}
