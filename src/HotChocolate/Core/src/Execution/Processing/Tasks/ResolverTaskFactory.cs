using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.PathHelper;
using static HotChocolate.Execution.Processing.ValueCompletion;

namespace HotChocolate.Execution.Processing.Tasks;

internal static class ResolverTaskFactory
{
    private static List<ResolverTask>? _pooled = [];

    static ResolverTaskFactory() { }

    public static ObjectResult EnqueueResolverTasks(
        OperationContext operationContext,
        ISelectionSet selectionSet,
        object? parent,
        Path path,
        IImmutableDictionary<string, object?> scopedContext,
        ObjectResult? parentResult = null)
    {
        var selectionsCount = selectionSet.Selections.Count;
        var responseIndex = selectionsCount;
        parentResult ??= operationContext.Result.RentObject(selectionsCount);
        var scheduler = operationContext.Scheduler;
        var includeFlags = operationContext.IncludeFlags;
        var final = !selectionSet.IsConditional;

        var bufferedTasks = Interlocked.Exchange(ref _pooled, null) ?? [];
        Debug.Assert(bufferedTasks.Count == 0, "The buffer must be clean.");

        try
        {
            ref var selectionSpace = ref ((SelectionSet)selectionSet).GetSelectionsReference();

            // we are iterating reverse so that in the case of a mutation the first
            // synchronous root selection is executed first, since the work scheduler
            // is using two stacks one for parallel work an one for synchronous work.
            // the scheduler this tries to schedule new work first.
            // coincidentally we can use that to schedule a mutation so that we honor the spec
            // guarantees while executing efficient.
            for (var i = selectionsCount - 1; i >= 0; i--)
            {
                ref var selection = ref Unsafe.Add(ref selectionSpace, i);

                if (final || selection.IsIncluded(includeFlags))
                {
                    bufferedTasks.Add(
                        operationContext.CreateResolverTask(
                            selection,
                            parent,
                            parentResult,
                            --responseIndex,
                            scopedContext));
                }
            }

            if (bufferedTasks.Count == 0)
            {
                // in the case all root fields are skipped we execute a dummy task in order
                // to not have to many extra API for this special case.
                scheduler.Register(new NoOpExecutionTask(operationContext));
            }
            else
            {
                scheduler.Register(CollectionsMarshal.AsSpan(bufferedTasks));
            }

            if (selectionSet.Fragments.Count > 0)
            {
                TryHandleDeferredFragments(
                    operationContext,
                    selectionSet,
                    scopedContext,
                    path,
                    parent,
                    parentResult);
            }

            return parentResult;
        }
        finally
        {
            bufferedTasks.Clear();
            Interlocked.Exchange(ref _pooled!, bufferedTasks);
        }
    }

    public static ResolverTask EnqueueElementTasks(
        OperationContext operationContext,
        ISelection selection,
        object? parent,
        Path path,
        int index,
        IAsyncEnumerator<object?> value,
        IImmutableDictionary<string, object?> scopedContext)
    {
        var parentResult = operationContext.Result.RentObject(1);
        var bufferedTasks = Interlocked.Exchange(ref _pooled, null) ?? [];
        Debug.Assert(bufferedTasks.Count == 0, "The buffer must be clean.");

        var resolverTask =
            operationContext.CreateResolverTask(
                selection,
                parent,
                parentResult,
                0,
                scopedContext,
                path.Append(index));

        try
        {
            CompleteInline(
                operationContext,
                resolverTask.Context,
                selection,
                selection.Type.ElementType(),
                0,
                parentResult,
                value.Current,
                bufferedTasks);

            // if we have child tasks we need to register them.
            if (bufferedTasks.Count > 0)
            {
                operationContext.Scheduler.Register(CollectionsMarshal.AsSpan(bufferedTasks));
            }
        }
        finally
        {
            bufferedTasks.Clear();
            Interlocked.Exchange(ref _pooled, bufferedTasks);
        }

        return resolverTask;
    }

    public static ObjectResult? EnqueueOrInlineResolverTasks(
        ValueCompletionContext context,
        ObjectType parentType,
        ResultData parentResult,
        int parentIndex,
        object parent,
        ISelectionSet selectionSet)
    {
        var responseIndex = 0;
        var selectionsCount = selectionSet.Selections.Count;
        var operationContext = context.OperationContext;
        var result = operationContext.Result.RentObject(selectionsCount);
        var includeFlags = operationContext.IncludeFlags;
        var final = !selectionSet.IsConditional;

        result.SetParent(parentResult, parentIndex);

        ref var selection = ref ((SelectionSet)selectionSet).GetSelectionsReference();
        ref var end = ref Unsafe.Add(ref selection, selectionsCount);

        while (Unsafe.IsAddressLessThan(ref selection, ref end))
        {
            if (result.IsInvalidated)
            {
                return null;
            }

            if (!final && !selection.IsIncluded(includeFlags))
            {
                goto NEXT;
            }

            if (selection.Strategy is SelectionExecutionStrategy.Pure)
            {
                ResolveAndCompleteInline(
                    context,
                    selection,
                    responseIndex++,
                    parentType,
                    parent,
                    result);
            }
            else
            {
                context.Tasks.Add(
                    operationContext.CreateResolverTask(
                        selection,
                        parent,
                        result,
                        responseIndex++,
                        context.ResolverContext.ScopedContextData));
            }

            NEXT:
            selection = ref Unsafe.Add(ref selection, 1)!;
        }

        if (selectionSet.Fragments.Count > 0)
        {
            TryHandleDeferredFragments(
                operationContext,
                selectionSet,
                context.ResolverContext.ScopedContextData,
                CreatePathFromContext(result),
                parent,
                result);
        }

        return result.IsInvalidated ? null : result;
    }

    private static void ResolveAndCompleteInline(
        ValueCompletionContext context,
        ISelection selection,
        int responseIndex,
        ObjectType parentType,
        object parent,
        ObjectResult parentResult)
    {
        var operationContext = context.OperationContext;
        var resolverContext = context.ResolverContext;
        var executedSuccessfully = false;
        object? resolverResult = null;

        parentResult.InitValueUnsafe(responseIndex, selection);

        try
        {
            // we first try to create a context for our pure resolver.
            // this should actually only fail if we are unable to coerce
            // the field arguments.
            if (resolverContext.TryCreatePureContext(
                selection, parentType, parentResult, parent,
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
            var path = CreatePathFromContext(selection, parentResult, responseIndex);
            operationContext.ReportError(ex, resolverContext, selection, path);
        }

        if (executedSuccessfully)
        {
            // if we were able to execute the resolver we will try to complete the
            // resolver result inline and commit the value to the result..
            CompleteInline(
                operationContext,
                resolverContext,
                selection,
                selection.Type,
                responseIndex,
                parentResult,
                resolverResult,
                context.Tasks);
        }
        else
        {
            // if we were not able to execute the resolver we will commit the null value
            // of the resolver to the object result which could trigger a non-null propagation.
            CommitValue(
                operationContext,
                selection,
                responseIndex,
                parentResult,
                resolverResult);
        }
    }

    private static void CompleteInline(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        IType type,
        int responseIndex,
        ObjectResult parentResult,
        object? value,
        List<ResolverTask> bufferedTasks)
    {
        object? completedValue = null;

        try
        {
            var completionContext = new ValueCompletionContext(operationContext, resolverContext, bufferedTasks);
            completedValue = Complete(completionContext, selection, type, parentResult, responseIndex, value);
        }
        catch (OperationCanceledException)
        {
            // If we run into this exception the request was aborted.
            // In this case we do nothing and just return.
            return;
        }
        catch (Exception ex)
        {
            var errorPath = CreatePathFromContext(selection, parentResult, responseIndex);
            operationContext.ReportError(ex, resolverContext, selection, errorPath);
        }

        CommitValue(operationContext, selection, responseIndex, parentResult, completedValue);
    }

    private static void CommitValue(
        OperationContext operationContext,
        ISelection selection,
        int responseIndex,
        ObjectResult parentResult,
        object? completedValue)
    {
        var isNonNullType = selection.Type.Kind is TypeKind.NonNull;

        parentResult.SetValueUnsafe(
            responseIndex,
            selection.ResponseName,
            completedValue,
            !isNonNullType);

        if (completedValue is null && isNonNullType)
        {
            PropagateNullValues(parentResult);
            var errorPath = CreatePathFromContext(selection, parentResult, responseIndex);
            operationContext.Result.AddNonNullViolation(selection, errorPath);
        }
    }

    private static void TryHandleDeferredFragments(
        OperationContext operationContext,
        ISelectionSet selectionSet,
        IImmutableDictionary<string, object?> scopedContext,
        Path path,
        object? parent,
        ObjectResult parentResult)
    {
        var fragments = selectionSet.Fragments;
        var includeFlags = operationContext.IncludeFlags;

        for (var i = 0; i < fragments.Count; i++)
        {
            var fragment = fragments[i];
            if (!fragment.IsConditional || fragment.IsIncluded(includeFlags))
            {
                operationContext.DeferredScheduler.Register(
                    new DeferredFragment(
                        fragment,
                        fragment.GetLabel(operationContext.Variables),
                        path,
                        parent,
                        scopedContext),
                    parentResult);
            }
        }
    }

    private sealed class NoOpExecutionTask : ExecutionTask
    {
        public NoOpExecutionTask(OperationContext context)
        {
            Context = context;
        }

        protected override IExecutionTaskContext Context { get; }

        protected override ValueTask ExecuteAsync(CancellationToken cancellationToken)
            => default;
    }
}
