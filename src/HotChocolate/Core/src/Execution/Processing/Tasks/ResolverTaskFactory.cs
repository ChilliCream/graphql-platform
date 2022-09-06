using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing.Tasks;

internal static class ResolverTaskFactory
{
    private static List<ResolverTask>? _pooled = new();

    public static ObjectResult EnqueueResolverTasks(
        OperationContext operationContext,
        ISelectionSet selectionSet,
        object? parent,
        Path path,
        IImmutableDictionary<string, object?> scopedContext)
    {
        var selectionsCount = selectionSet.Selections.Count;
        var responseIndex = selectionsCount;
        var parentResult = operationContext.Result.RentObject(selectionsCount);
        var scheduler = operationContext.Scheduler;
        var pathFactory = operationContext.PathFactory;
        var includeFlags = operationContext.IncludeFlags;
        var final = !selectionSet.IsConditional;

        var bufferedTasks = Interlocked.Exchange(ref _pooled, null) ?? new();
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
                            pathFactory.Append(path, selection.ResponseName),
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
                scheduler.Register(bufferedTasks);
            }

            if (selectionSet.Fragments.Count > 0)
            {
                TryHandleDeferredFragments(
                    operationContext,
                    selectionSet,
                    scopedContext,
                    path,
                    parent);
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
        var bufferedTasks = Interlocked.Exchange(ref _pooled, null) ?? new();
        Debug.Assert(bufferedTasks.Count == 0, "The buffer must be clean.");

        var resolverTask =
            operationContext.CreateResolverTask(
                selection,
                parent,
                parentResult,
                0,
                path,
                scopedContext);

        try
        {
            CompleteInline(
                operationContext,
                resolverTask.Context,
                selection,
                selection.Type.ElementType(),
                operationContext.PathFactory.Append(path, index),
                0,
                parentResult,
                value.Current,
                bufferedTasks);
        }
        finally
        {
            bufferedTasks.Clear();
            Interlocked.Exchange(ref _pooled, bufferedTasks);
        }

        return resolverTask;
    }

    public static ObjectResult EnqueueOrInlineResolverTasks(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        Path path,
        ObjectType parentType,
        object parent,
        ISelectionSet selectionSet,
        List<ResolverTask> bufferedTasks)
    {
        var responseIndex = 0;
        var selectionsCount = selectionSet.Selections.Count;
        var parentResult = operationContext.Result.RentObject(selectionsCount);
        var pathFactory = operationContext.PathFactory;
        var includeFlags = operationContext.IncludeFlags;
        var final = !selectionSet.IsConditional;

        ref var selectionSpace = ref ((SelectionSet)selectionSet).GetSelectionsReference();

        for (var i = 0; i < selectionsCount; i++)
        {
            ref var selection = ref Unsafe.Add(ref selectionSpace, i);

            if (final || selection.IsIncluded(includeFlags))
            {
                var selectionPath = pathFactory.Append(path, selection.ResponseName);

                if (selection.Strategy is SelectionExecutionStrategy.Pure)
                {
                    ResolveAndCompleteInline(
                        operationContext,
                        resolverContext,
                        selection,
                        selectionPath,
                        responseIndex++,
                        parentType,
                        parent,
                        parentResult,
                        bufferedTasks);
                }
                else
                {
                    bufferedTasks.Add(
                        operationContext.CreateResolverTask(
                            selection,
                            parent,
                            parentResult,
                            responseIndex++,
                            selectionPath,
                            resolverContext.ScopedContextData));
                }
            }
        }

        if (selectionSet.Fragments.Count > 0)
        {
            TryHandleDeferredFragments(
                operationContext,
                selectionSet,
                resolverContext.ScopedContextData,
                path,
                parent);
        }

        return parentResult;
    }

    private static void ResolveAndCompleteInline(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        int responseIndex,
        ObjectType parentType,
        object parent,
        ObjectResult parentResult,
        List<ResolverTask> bufferedTasks)
    {
        var executedSuccessfully = false;
        object? resolverResult = null;

        try
        {
            // we first try to create a context for our pure resolver.
            // this should actually only fail if we are unable to coerce
            // the field arguments.
            if (resolverContext.TryCreatePureContext(
                selection, path, parentType, parent,
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
                path,
                responseIndex,
                parentResult,
                resolverResult,
                bufferedTasks);
        }
        else
        {
            // if we were not able to execute the resolver we will commit the null value
            // of the resolver to the object result which could trigger a non-null propagation.
            CommitValue(
                operationContext,
                selection,
                path,
                responseIndex,
                parentResult,
                resolverResult);
        }
    }

    private static void CompleteInline(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        IType selectionType,
        Path path,
        int responseIndex,
        ObjectResult resultMap,
        object? value,
        List<ResolverTask> bufferedTasks)
    {
        object? completedValue = null;

        try
        {
            completedValue = ValueCompletion.Complete(
                operationContext,
                resolverContext,
                bufferedTasks,
                selection,
                path,
                selectionType,
                selection.ResponseName,
                responseIndex,
                value);

            if (completedValue is ResultData result)
            {
                result.Parent = resultMap;
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
            operationContext.ReportError(ex, resolverContext, selection, path);
        }

        CommitValue(
            operationContext,
            selection,
            path,
            responseIndex,
            resultMap,
            completedValue);
    }

    private static void CommitValue(
        OperationContext operationContext,
        ISelection selection,
        Path path,
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
            // if we detect a non-null violation we will stash it for later.
            // the non-null propagation is delayed so that we can parallelize better.
            operationContext.Result.AddNonNullViolation(selection, path, parentResult);
        }
    }

    private static void TryHandleDeferredFragments(
        OperationContext operationContext,
        ISelectionSet selectionSet,
        IImmutableDictionary<string, object?> scopedContext,
        Path path,
        object? parent)
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
                        path.Clone(),
                        parent,
                        scopedContext));
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
