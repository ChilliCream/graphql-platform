using System.Collections.Immutable;
using System.Buffers;
using System.Diagnostics;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.ValueCompletion;

namespace HotChocolate.Execution.Processing.Tasks;

internal static class ResolverTaskFactory
{
    private static readonly ArrayPool<IExecutionTask> s_pool = ArrayPool<IExecutionTask>.Shared;

    public static void EnqueueRootResolverTasks(
        OperationContext operationContext,
        object? parent,
        ResultElement resultValue,
        IImmutableDictionary<string, object?> scopedContext)
    {
        var selectionSet = resultValue.AssertSelectionSet();
        var scheduler = operationContext.Scheduler;
        var bufferedTasks = s_pool.Rent(resultValue.GetPropertyCount());
        var mainBranchId = operationContext.ExecutionBranchId;
        var data = resultValue.EnumerateObject();
        var i = 0;

        try
        {
            if (selectionSet.HasIncrementalParts)
            {
                var coordinator = operationContext.DeferExecutionCoordinator;
                var deferFlags = operationContext.DeferConditionFlags;
                var branches = ImmutableDictionary<DeferUsage, int>.Empty;

                foreach (var field in data)
                {
                    var selection = field.AssertSelection();

                    if (selection.IsDeferred(deferFlags))
                    {
                        // Get all active defer usages for this field.
                        // If IsDeferred is true, there is at least one active usage.
                        var deferUsages = selection.GetActiveDeferUsages(deferFlags);
                        Debug.Assert(deferUsages is not null);

                        field.Value.MarkAsDeferred();

                        // Create a branch for each distinct active defer usage.
                        foreach (var deferUsage in deferUsages)
                        {
                            if (!branches.TryGetValue(deferUsage, out _))
                            {
                                var branchId = coordinator.Branch(mainBranchId, Path.Root, deferUsage);
                                branches = branches.Add(deferUsage, branchId);
                            }
                        }

                        continue;
                    }

                    if (selection.Strategy is SelectionExecutionStrategy.Batch)
                    {
                        scheduler.RegisterBatchEntry(
                            selection,
                            parent,
                            field.Value,
                            scopedContext,
                            mainBranchId);
                    }
                    else
                    {
                        bufferedTasks[i++] =
                            operationContext.CreateResolverTask(
                                parent,
                                selection,
                                field.Value,
                                scopedContext);
                    }
                }

                if (i == 0 && branches.IsEmpty)
                {
                    // in the case all root fields are skipped we execute a dummy task in order
                    // to not have extra logic for this case.
                    scheduler.Register(new NoOpExecutionTask(operationContext));
                }
                else
                {
                    if (i > 0)
                    {
                        scheduler.Register(bufferedTasks.AsSpan(0, i));
                    }

                    if (!branches.IsEmpty)
                    {
                        foreach (var (deferUsage, branchId) in branches)
                        {
                            scheduler.Register(
                                operationContext.CreateDeferTask(
                                    selectionSet,
                                    Path.Root,
                                    parent,
                                    scopedContext,
                                    groupScope: null,
                                    branchId,
                                    deferUsage));
                        }
                    }
                }
            }
            else
            {
                foreach (var field in data)
                {
                    var selection = field.AssertSelection();

                    if (selection.Strategy is SelectionExecutionStrategy.Batch)
                    {
                        scheduler.RegisterBatchEntry(
                            selection,
                            parent,
                            field.Value,
                            scopedContext,
                            mainBranchId);
                    }
                    else
                    {
                        bufferedTasks[i++] =
                            operationContext.CreateResolverTask(
                                parent,
                                selection,
                                field.Value,
                                scopedContext);
                    }
                }

                if (i == 0)
                {
                    // in the case all root fields are skipped we execute a dummy task in order
                    // to not have extra logic for this case.
                    scheduler.Register(new NoOpExecutionTask(operationContext));
                }
                else
                {
                    scheduler.Register(bufferedTasks.AsSpan(0, i));
                }
            }
        }
        finally
        {
            if (i > 0)
            {
                bufferedTasks.AsSpan(0, i).Clear();
            }

            s_pool.Return(bufferedTasks);
        }
    }

    public static void EnqueueOrInlineResolverTasks(
        ValueCompletionContext context,
        SelectionSet selectionSet,
        ObjectType selectionSetType,
        ResultElement resultValue,
        object parent)
    {
        Debug.Assert(selectionSet.Type == selectionSetType);
        Debug.Assert(resultValue.Type?.NamedType()?.IsAssignableFrom(selectionSetType) ?? false);

        var operationContext = context.OperationContext;
        var parentDeferUsage = context.ResolverContext.DeferUsage;

        resultValue.SetObjectValue(selectionSet);

        if (selectionSet.HasIncrementalParts)
        {
            var coordinator = operationContext.DeferExecutionCoordinator;
            var deferFlags = operationContext.DeferConditionFlags;
            var branches = ImmutableDictionary<DeferUsage, int>.Empty;
            Path? currentPath = null;

            var parentBranchId = context.ParentBranchId;

            foreach (var field in resultValue.EnumerateObject())
            {
                var selection = field.AssertSelection();

                if (selection.IsDeferred(deferFlags, parentDeferUsage))
                {
                    var deferUsages = selection.GetActiveDeferUsages(deferFlags);
                    Debug.Assert(deferUsages is not null);

                    field.Value.MarkAsDeferred();

                    // Only create branches for defer usages that are descendants
                    // of the parent defer usage. Sibling defers are handled by
                    // their own DeferTask at a higher level.
                    foreach (var deferUsage in deferUsages)
                    {
                        if (!IsDescendantOf(deferUsage, parentDeferUsage))
                        {
                            continue;
                        }

                        if (!branches.TryGetValue(deferUsage, out _))
                        {
                            currentPath ??= resultValue.Path;
                            var branchId = coordinator.Branch(parentBranchId, currentPath, deferUsage);
                            branches = branches.Add(deferUsage, branchId);
                            context.Tasks.Add(
                                operationContext.CreateDeferTask(
                                    selectionSet,
                                    currentPath,
                                    parent,
                                    context.ResolverContext.ScopedContextData,
                                    context.GroupScope,
                                    branchId,
                                    deferUsage));
                        }
                    }
                }
                else if (IsInline(context, selection))
                {
                    ResolveAndCompleteInline(
                        context,
                        selection,
                        selectionSetType,
                        field.Value,
                        parent);
                }
                else if (selection.Strategy is SelectionExecutionStrategy.Batch)
                {
                    operationContext.Scheduler.RegisterBatchEntry(
                        selection,
                        parent,
                        field.Value,
                        context.ResolverContext.ScopedContextData,
                        context.ParentBranchId,
                        parentDeferUsage);
                }
                else
                {
                    context.Tasks.Add(
                        CreateChildTask(
                            context,
                            parent,
                            selection,
                            field.Value,
                            parentDeferUsage));
                }
            }
        }
        else
        {
            foreach (var field in resultValue.EnumerateObject())
            {
                var selection = field.AssertSelection();

                if (IsInline(context, selection))
                {
                    ResolveAndCompleteInline(
                        context,
                        selection,
                        selectionSetType,
                        field.Value,
                        parent);
                }
                else if (selection.Strategy is SelectionExecutionStrategy.Batch)
                {
                    operationContext.Scheduler.RegisterBatchEntry(
                        selection,
                        parent,
                        field.Value,
                        context.ResolverContext.ScopedContextData,
                        context.ParentBranchId);
                }
                else
                {
                    context.Tasks.Add(
                        CreateChildTask(
                            context,
                            parent,
                            selection,
                            field.Value,
                            deferUsage: null));
                }
            }
        }
    }

    private static bool IsInline(ValueCompletionContext context, Selection selection)
        => selection.Strategy is SelectionExecutionStrategy.Pure
            && (context.GroupScope is null || !context.GroupScope.IsMember(selection));

    private static ResolverTask CreateChildTask(
        ValueCompletionContext context,
        object parent,
        Selection selection,
        ResultElement resultValue,
        DeferUsage? deferUsage)
    {
        var task =
            context.OperationContext.CreateResolverTask(
                parent,
                selection,
                resultValue,
                context.ResolverContext.ScopedContextData,
                context.ParentBranchId,
                deferUsage);

        task.JoinExecutionGroup(context.GroupScope);

        return task;
    }

    private static void ResolveAndCompleteInline(
        ValueCompletionContext context,
        Selection selection,
        ObjectType selectionSetType,
        ResultElement fieldValue,
        object parent)
    {
        var operationContext = context.OperationContext;
        var resolverContext = context.ResolverContext;
        var groupScope = context.GroupScope;
        var taskCount = groupScope is null ? 0 : context.Tasks.Count;
        var executedSuccessfully = false;
        object? resolverResult = null;

        try
        {
            // we first try to create a context for our pure resolver.
            // this should actually only fail if we are unable to coerce
            // the field arguments.
            if (resolverContext.TryCreatePureContext(
                selection,
                selectionSetType,
                fieldValue,
                parent,
                out var childContext))
            {
                // if we have a pure context we can execute out pure resolver.
                resolverResult = selection.PureResolver!(childContext);
                executedSuccessfully = true;
            }
        }
        catch (OperationCanceledException)
        {
            // If we run into this exception the request was aborted.
            // In this case we do nothing and just return.
            ReleaseWhenNoChildTasks(context, groupScope, selection, taskCount);
            return;
        }
        catch (Exception ex)
        {
            operationContext.ReportError(ex, resolverContext, fieldValue.Path);
        }

        if (!executedSuccessfully)
        {
            fieldValue.SetNullValue();
        }

        try
        {
            Complete(context, selection, fieldValue, resolverResult);
        }
        catch (OperationCanceledException)
        {
            // If we run into this exception the request was aborted.
            // In this case we do nothing and just return.
            ReleaseWhenNoChildTasks(context, groupScope, selection, taskCount);
            return;
        }
        catch (Exception ex)
        {
            operationContext.ReportError(ex, resolverContext, fieldValue.Path);
        }

        ReleaseWhenNoChildTasks(context, groupScope, selection, taskCount);

        if (fieldValue is { IsNullable: false, IsNullOrInvalidated: true })
        {
            if (operationContext.PropagateNullValues)
            {
                PropagateNullValues(fieldValue);
            }
            else
            {
                fieldValue.SetNullValue();
            }

            operationContext.Result.AddNonNullViolation(fieldValue.Path);
        }
    }

    private static void ReleaseWhenNoChildTasks(
        ValueCompletionContext context,
        ExecutionGroupScope? groupScope,
        Selection selection,
        int taskCount)
    {
        if (groupScope is not null && context.Tasks.Count == taskCount)
        {
            // without child tasks nothing below this selection can ever start, so its members are skipped.
            groupScope.ReleaseSlotsBelow(selection);
        }
    }

    /// <summary>
    /// Checks whether <paramref name="deferUsage"/> is a descendant of
    /// <paramref name="parentDeferUsage"/> in the defer usage parent chain.
    /// Returns <c>true</c> if <paramref name="parentDeferUsage"/> is <c>null</c>
    /// (everything is a descendant of the root context).
    /// </summary>
    private static bool IsDescendantOf(DeferUsage deferUsage, DeferUsage? parentDeferUsage)
    {
        if (parentDeferUsage is null)
        {
            return true;
        }

        var current = deferUsage.Parent;

        while (current is not null)
        {
            if (current == parentDeferUsage)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private sealed class NoOpExecutionTask(OperationContext context) : ExecutionTask
    {
        public override int BranchId => context.ExecutionBranchId;

        public override bool IsDeferred => false;

        protected override IExecutionTaskContext Context { get; } = context;

        protected override ValueTask ExecuteAsync(CancellationToken cancellationToken)
            => default;
    }
}
