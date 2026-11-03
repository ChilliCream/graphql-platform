using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Text.Json;

namespace HotChocolate.Execution.Processing;

internal sealed partial class WorkScheduler
{
    private readonly Dictionary<SelectionPath, int> _activePaths = [];
    private readonly Dictionary<(SelectionPath Path, DeferUsage? Defer, OperationContext Root), BatchResolverTask> _pendingBatches = [];

    /// <summary>
    /// Registers work of the specified operation context to be executed as part of a batch
    /// resolver task, which is shared by all contexts with the same root context.
    /// </summary>
    public void RegisterBatchEntry(
        OperationContext context,
        Selection selection,
        object? parent,
        ResultElement resultValue,
        IImmutableDictionary<string, object?> scopedContextData,
        int branchId,
        DeferUsage? deferUsage = null)
    {
        AssertNotPooled();

        var key = (selection.FieldSelectionPath, deferUsage, context.RootContext);

        lock (_sync)
        {
            if (!_pendingBatches.TryGetValue(key, out var batchTask))
            {
                batchTask =
                    context.CreateBatchResolverTask(
                        selection.Field,
                        selection.FieldSelectionPath,
                        branchId,
                        deferUsage);
                _pendingBatches[key] = batchTask;
                IncrementPathCountUnsafe(selection.FieldSelectionPath);
            }

            if (batchTask.AddEntry(context, parent, selection, resultValue, scopedContextData, branchId))
            {
                RegisterBranchTaskUnsafe(branchId);
            }
        }
    }

    /// <summary>
    /// Marks a deferred task's resolver registration as complete.
    /// </summary>
    public void CompleteDeferRegistration(DeferTask task)
    {
        AssertNotPooled();

        lock (_sync)
        {
            CompleteDeferRegistrationUnsafe(task);
        }
    }

    private void CompleteDeferRegistrationUnsafe(DeferTask task)
    {
        AssertLockHeld();

        if (task.IsResolverRegistrationPending)
        {
            task.IsResolverRegistrationPending = false;
            DecrementPathCountUnsafe(task.SelectionSetPath);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementPathCountUnsafe(SelectionPath path)
    {
        AssertLockHeld();

        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(_activePaths, path, out _);
        count++;
    }

    /// <summary>
    /// Decrements the active task count for the given path and checks if any
    /// pending batch tasks can now be dispatched.
    /// </summary>
    private void DecrementPathCountUnsafe(SelectionPath path)
    {
        AssertLockHeld();

        ref var count = ref CollectionsMarshal.GetValueRefOrNullRef(_activePaths, path);

        if (!Unsafe.IsNullRef(ref count) && --count <= 0)
        {
            _activePaths.Remove(path);
            TryDispatchPendingBatchesUnsafe(isIdle: false);
        }
    }

    /// <summary>
    /// Checks all pending batch tasks and dispatches any whose ancestor paths
    /// all have zero active tasks, or all of them if <paramref name="isIdle"/> is <c>true</c>.
    /// </summary>
    private void TryDispatchPendingBatchesUnsafe(bool isIdle)
    {
        AssertLockHeld();

        if (_pendingBatches.Count == 0)
        {
            return;
        }

        List<(SelectionPath Path, DeferUsage? Defer, OperationContext Root)>? toRemove = null;

        foreach (var (key, batchTask) in _pendingBatches)
        {
            if (!isIdle && !CanDispatchBatchUnsafe(key.Path))
            {
                continue;
            }

            toRemove ??= [];
            toRemove.Add(key);

            batchTask.Id = Interlocked.Increment(ref _nextId);
            batchTask.IsRegistered = true;
            _work.Push(batchTask);
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
        AssertLockHeld();

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
