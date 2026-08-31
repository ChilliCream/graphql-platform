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
    public async ValueTask ResetAsync()
    {
        List<OperationResult>? cleanup = null;

        lock (_sync)
        {
            if (_completedResults.Count > 0)
            {
                cleanup ??= [];
                cleanup.AddRange(_completedResults.Values);
            }

            foreach (var branch in _branchLookup.Values)
            {
                if (branch.Results is { Count: > 0 } results)
                {
                    cleanup ??= [];
                    cleanup.AddRange(results);
                }
            }

            if (_results.Count > 0)
            {
                cleanup ??= [];
                cleanup.AddRange(_results);
            }

            ResetUnsafe();
        }

        if (cleanup is not null)
        {
            foreach (var result in cleanup)
            {
                await result.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            if (_completedResults.Count > 0 || _results.Count > 0 || HasBufferedResultsUnsafe())
            {
                throw new InvalidOperationException(
                    "The coordinator has results that must be cleaned asynchronously before it can be reset.");
            }

            ResetUnsafe();
        }
    }

    private bool HasBufferedResultsUnsafe()
    {
        foreach (var branch in _branchLookup.Values)
        {
            if (branch.Results is { Count: > 0 })
            {
                return true;
            }
        }

        return false;
    }

    private void ResetUnsafe()
    {
        _branchIdLookup.Clear();
        _streamBranchIdLookup.Clear();
        _branchLookup.Clear();
        _mainBranchChildren?.Clear();
        _completedResults.Clear();
        _announced.Clear();
        _completedBranches.Clear();
        _results.Clear();
        _branchTracker = null!;
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
