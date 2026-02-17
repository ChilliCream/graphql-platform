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
                var deferFlags = operationContext.DeferFlags;
                var branches = ImmutableDictionary<DeferUsage, int>.Empty;
                DeferUsage? lastDeferUsage = null;

                foreach (var field in data)
                {
                    var selection = field.AssertSelection();

                    if (selection.IsDeferred(deferFlags))
                    {
                        // if IsDeferred is true then GetPrimaryDeferUsage will be guaranteed
                        // to return a defer usage for the same deferFlags
                        var deferUsage = selection.GetPrimaryDeferUsage(deferFlags);
                        Debug.Assert(deferUsage is not null);

                        field.Value.MarkAsDeferred();

                        if (lastDeferUsage == deferUsage)
                        {
                            continue;
                        }

                        if (!branches.TryGetValue(deferUsage, out var branchId))
                        {
                            branchId = coordinator.Branch(mainBranchId, Path.Root, deferUsage);
                            branches = branches.Add(deferUsage, branchId);
                        }

                        lastDeferUsage = deferUsage;
                        continue;
                    }

                    bufferedTasks[i++] =
                        operationContext.CreateResolverTask(
                            parent,
                            selection,
                            field.Value,
                            scopedContext);
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
                    bufferedTasks[i++] =
                        operationContext.CreateResolverTask(
                            parent,
                            field.AssertSelection(),
                            field.Value,
                            scopedContext);
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
            var deferFlags = operationContext.DeferFlags;
            var branches = ImmutableDictionary<DeferUsage, int>.Empty;
            DeferUsage? lastDeferUsage = null;
            Path? currentPath = null;

            var parentBranchId = context.ParentBranchId;

            foreach (var field in resultValue.EnumerateObject())
            {
                var selection = field.AssertSelection();

                if (selection.IsDeferred(deferFlags, parentDeferUsage))
                {
                    // if IsDeferred is true then GetPrimaryDeferUsage will be guaranteed
                    // to return a defer usage for the same deferFlags
                    var deferUsage = selection.GetPrimaryDeferUsage(deferFlags);
                    Debug.Assert(deferUsage is not null);

                    field.Value.MarkAsDeferred();

                    if (lastDeferUsage == deferUsage)
                    {
                        continue;
                    }

                    if (!branches.TryGetValue(deferUsage, out var branchId))
                    {
                        currentPath ??= resultValue.Path;
                        branchId = coordinator.Branch(parentBranchId, currentPath, deferUsage);
                        branches = branches.Add(deferUsage, branchId);
                        context.Tasks.Add(
                            operationContext.CreateDeferTask(
                                selectionSet,
                                currentPath,
                                parent,
                                context.ResolverContext.ScopedContextData,
                                branchId,
                                deferUsage));
                    }

                    lastDeferUsage = deferUsage;
                }
                else if (selection.Strategy is SelectionExecutionStrategy.Pure)
                {
                    ResolveAndCompleteInline(
                        context,
                        selection,
                        selectionSetType,
                        field.Value,
                        parent);
                }
                else
                {
                    context.Tasks.Add(
                        operationContext.CreateResolverTask(
                            parent,
                            selection,
                            field.Value,
                            context.ResolverContext.ScopedContextData));
                }
            }
        }
        else
        {
            foreach (var field in resultValue.EnumerateObject())
            {
                var selection = field.AssertSelection();

                if (selection.Strategy is SelectionExecutionStrategy.Pure)
                {
                    ResolveAndCompleteInline(
                        context,
                        selection,
                        selectionSetType,
                        field.Value,
                        parent);
                }
                else
                {
                    context.Tasks.Add(
                        operationContext.CreateResolverTask(
                            parent,
                            selection,
                            field.Value,
                            context.ResolverContext.ScopedContextData));
                }
            }
        }
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
            return;
        }
        catch (Exception ex)
        {
            operationContext.ReportError(ex, resolverContext, selection, fieldValue.Path);
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
            return;
        }
        catch (Exception ex)
        {
            operationContext.ReportError(ex, resolverContext, selection, fieldValue.Path);
        }

        if (fieldValue is { IsNullable: false, IsNullOrInvalidated: true })
        {
            PropagateNullValues(fieldValue);
            operationContext.Result.AddNonNullViolation(fieldValue.Path);
        }
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
