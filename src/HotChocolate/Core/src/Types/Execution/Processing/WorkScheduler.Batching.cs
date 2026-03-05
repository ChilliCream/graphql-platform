using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal sealed partial class WorkScheduler
{
    private readonly Dictionary<SelectionPath, int> _activePaths = [];
    private readonly Dictionary<(SelectionPath Path, DeferUsage? Defer), BatchResolverTask> _pendingBatches = [];

    /// <summary>
    /// Gets or creates a <see cref="BatchResolverTask"/> for the given selection path
    /// and defer usage combination. Entries with different defer usages are kept in
    /// separate batches so that each batch can be delivered under the correct
    /// <c>@defer</c> boundary.
    /// Called during value completion when a batch field is encountered.
    /// The task is held in the pending batches registry until all ancestor
    /// paths have completed, at which point it is moved to the work queue.
    /// </summary>
    public BatchResolverTask GetOrCreateBatchTask(
        SelectionPath selectionPath,
        ObjectField field,
        int branchId,
        DeferUsage? deferUsage = null)
    {
        AssertNotPooled();

        var key = (selectionPath, deferUsage);

        lock (_sync)
        {
            if (!_pendingBatches.TryGetValue(key, out var batchTask))
            {
                batchTask = operationContext.CreateBatchResolverTask(field, selectionPath, branchId, deferUsage);
                _pendingBatches[key] = batchTask;
                IncrementPathCountUnsafe(selectionPath);
            }

            return batchTask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementPathCountUnsafe(SelectionPath path)
    {
        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(_activePaths, path, out _);
        count++;
    }

    /// <summary>
    /// Decrements the active task count for the given path and checks if any
    /// pending batch tasks can now be dispatched.
    /// </summary>
    private void DecrementPathCountUnsafe(SelectionPath path)
    {
        ref var count = ref CollectionsMarshal.GetValueRefOrNullRef(_activePaths, path);

        if (!Unsafe.IsNullRef(ref count) && --count <= 0)
        {
            _activePaths.Remove(path);
            TryDispatchPendingBatchesUnsafe();
        }
    }

    /// <summary>
    /// Checks all pending batch tasks and dispatches any whose ancestor paths
    /// all have zero active tasks.
    /// </summary>
    private void TryDispatchPendingBatchesUnsafe()
    {
        if (_pendingBatches.Count == 0)
        {
            return;
        }

        List<(SelectionPath Path, DeferUsage? Defer)>? toRemove = null;

        foreach (var (key, batchTask) in _pendingBatches)
        {
            if (!CanDispatchBatchUnsafe(key.Path))
            {
                continue;
            }

            toRemove ??= [];
            toRemove.Add(key);

            batchTask.Id = Interlocked.Increment(ref _nextId);
            batchTask.IsRegistered = true;
            _work.Push(batchTask);
            RegisterBranchTaskUnsafe(batchTask.BranchId);
        }

        if (toRemove is null)
        {
            return;
        }

        foreach (var key in toRemove)
        {
            _pendingBatches.Remove(key);
        }

        _signal.Set();
    }

    /// <summary>
    /// Determines whether a batch task at the given path can be dispatched.
    /// A batch is dispatchable when all strict ancestor paths have zero active tasks.
    /// Walks the cached parent chain on <see cref="SelectionPath"/> — no allocation.
    /// </summary>
    private bool CanDispatchBatchUnsafe(SelectionPath batchPath)
    {
        // Walk up ancestor paths. If any ancestor still has active tasks,
        // more entries could still be added to this batch.
        var ancestor = batchPath.Parent;

        while (ancestor is not null)
        {
            if (_activePaths.TryGetValue(ancestor, out var count) && count > 0)
            {
                return false;
            }

            ancestor = ancestor.Parent;
        }

        return true;
    }
}
