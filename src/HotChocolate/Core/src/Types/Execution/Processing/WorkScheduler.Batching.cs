using System.Collections.Immutable;
using System.Diagnostics;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Text.Json;

namespace HotChocolate.Execution.Processing;

internal sealed partial class WorkScheduler
{
    // Tracks running + queued task count per batch selection path.
    private readonly Dictionary<BatchSelectionPath, int> _activeCountByPath = new();

    // Batch resolver tasks waiting to dispatch, keyed by their selection path.
    private readonly Dictionary<BatchSelectionPath, BatchResolverTask> _pendingBatches = new();

    /// <summary>
    /// Gets or creates a <see cref="BatchResolverTask"/> for the given selection path.
    /// Called during value completion when a batch field is encountered.
    /// The task is held in the pending batches registry until all ancestor
    /// paths have completed, at which point it is moved to the work queue.
    /// </summary>
    public BatchResolverTask GetOrCreateBatchTask(
        BatchSelectionPath selectionPath,
        Selection selection,
        BatchFieldDelegate pipeline,
        int branchId)
    {
        AssertNotPooled();

        lock (_sync)
        {
            if (!_pendingBatches.TryGetValue(selectionPath, out var batchTask))
            {
                batchTask = new BatchResolverTask();
                batchTask.Initialize(operationContext, selection, pipeline, selectionPath, branchId);
                _pendingBatches[selectionPath] = batchTask;
            }

            return batchTask;
        }
    }

    /// <summary>
    /// Increments the active task count for the given path.
    /// Must be called under <c>_sync</c> lock.
    /// </summary>
    private void IncrementPathCountUnsafe(BatchSelectionPath? path)
    {
        if (path is null)
        {
            return;
        }

        // Increment for this path and all its ancestors
        var current = path;

        while (current is not null)
        {
            if (_activeCountByPath.TryGetValue(current, out var count))
            {
                _activeCountByPath[current] = count + 1;
            }
            else
            {
                _activeCountByPath[current] = 1;
            }

            current = current.Parent;
        }
    }

    /// <summary>
    /// Decrements the active task count for the given path and checks if any
    /// pending batch tasks can now be dispatched.
    /// Must be called under <c>_sync</c> lock.
    /// </summary>
    private void DecrementPathCountUnsafe(BatchSelectionPath? path)
    {
        if (path is null)
        {
            return;
        }

        // Decrement for this path and all its ancestors
        var current = path;

        while (current is not null)
        {
            if (_activeCountByPath.TryGetValue(current, out var count))
            {
                if (count <= 1)
                {
                    _activeCountByPath.Remove(current);
                }
                else
                {
                    _activeCountByPath[current] = count - 1;
                }
            }

            current = current.Parent;
        }

        // Check if any pending batches can now be dispatched.
        TryDispatchPendingBatchesUnsafe();
    }

    /// <summary>
    /// Checks all pending batch tasks and dispatches any whose ancestor paths
    /// all have zero active tasks.
    /// Must be called under <c>_sync</c> lock.
    /// </summary>
    private void TryDispatchPendingBatchesUnsafe()
    {
        if (_pendingBatches.Count == 0)
        {
            return;
        }

        // Collect dispatchable batches (can't modify dictionary during iteration)
        List<BatchSelectionPath>? toDispatch = null;

        foreach (var (batchPath, batchTask) in _pendingBatches)
        {
            if (CanDispatchBatchUnsafe(batchPath))
            {
                toDispatch ??= [];
                toDispatch.Add(batchPath);
            }
        }

        if (toDispatch is null)
        {
            return;
        }

        foreach (var path in toDispatch)
        {
            var batchTask = _pendingBatches[path];
            _pendingBatches.Remove(path);

            // Move the batch task to the work queue
            batchTask.Id = Interlocked.Increment(ref _nextId);
            batchTask.IsRegistered = true;
            _work.Push(batchTask);
            RegisterBranchTaskUnsafe(batchTask.BranchId);
        }

        // Signal the execution loop that new work is available.
        _signal.Set();
    }

    /// <summary>
    /// Determines whether a batch task at the given path can be dispatched.
    /// A batch is dispatchable when all strict ancestor paths have zero active tasks.
    /// Must be called under <c>_sync</c> lock.
    /// </summary>
    private bool CanDispatchBatchUnsafe(BatchSelectionPath batchPath)
    {
        // Walk up ancestor paths. If any ancestor still has active tasks,
        // more entries could still be added to this batch.
        var ancestor = batchPath.Parent;

        while (ancestor is not null)
        {
            if (_activeCountByPath.TryGetValue(ancestor, out var count) && count > 0)
            {
                return false;
            }

            ancestor = ancestor.Parent;
        }

        return true;
    }
}
