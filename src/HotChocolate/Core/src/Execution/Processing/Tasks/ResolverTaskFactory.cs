using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing.Tasks;

internal static class ResolverTaskFactory
{
    private static List<ResolverTask>? _pooled = new();

    public static ObjectResult EnqueueResolverTasks(
        IOperationContext operationContext,
        ISelectionSet selectionSet,
        object? parent,
        Path path,
        IImmutableDictionary<string, object?> scopedContext)
    {
        var responseIndex = 0;
        var selections = selectionSet.Selections;
        var parentResult = operationContext.Result.RentObject(selections.Count);
        var scheduler = operationContext.Scheduler;
        var includeFlags = operationContext.IncludeFlags;
        var final = !selectionSet.IsConditional;

        var bufferedTasks = Interlocked.Exchange(ref _pooled, null) ?? new();
        Debug.Assert(bufferedTasks.Count == 0, "The buffer must be clean.");

        try
        {
            for (var i = 0; i < selections.Count; i++)
            {
                var selection = selections[i];
                if (final || selection.IsIncluded(includeFlags))
                {
                    bufferedTasks.Add(CreateResolverTask(
                        operationContext,
                        selection,
                        parent,
                        responseIndex++,
                        operationContext.PathFactory.Append(path, selection.ResponseName),
                        parentResult,
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

            TryHandleDeferredFragments(
                operationContext,
                selectionSet,
                scopedContext,
                path,
                parent);

            return parentResult;
        }
        finally
        {
            bufferedTasks.Clear();
            Interlocked.Exchange(ref _pooled, bufferedTasks);
        }
    }

    public static ResolverTask EnqueueElementTasks(
        IOperationContext operationContext,
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

        var resolverTask = CreateResolverTask(
            operationContext,
            selection,
            parent,
            0,
            path,
            parentResult,
            scopedContext);

        try
        {
            CompleteInline(
                operationContext,
                resolverTask.ResolverContext,
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
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        Path path,
        ObjectType parentType,
        object parent,
        ISelectionSet selectionSet,
        List<ResolverTask> bufferedTasks)
    {
        var responseIndex = 0;
        var selections = selectionSet.Selections;
        var parentResult = operationContext.Result.RentObject(selections.Count);
        var includeFlags = operationContext.IncludeFlags;
        var final = !selectionSet.IsConditional;

        for (var i = 0; i < selections.Count; i++)
        {
            var selection = selections[i];

            if (final || selection.IsIncluded(includeFlags))
            {
                if (selection.Strategy is SelectionExecutionStrategy.Pure)
                {
                    ResolveAndCompleteInline(
                        operationContext,
                        resolverContext,
                        selection,
                        operationContext.PathFactory.Append(path, selection.ResponseName),
                        responseIndex++,
                        parentType,
                        parent,
                        parentResult,
                        bufferedTasks);
                }
                else
                {
                    bufferedTasks.Add(CreateResolverTask(
                        operationContext,
                        resolverContext,
                        selection,
                        operationContext.PathFactory.Append(path, selection.ResponseName),
                        responseIndex++,
                        parent,
                        parentResult));
                }
            }
        }

        TryHandleDeferredFragments(
            operationContext,
            selectionSet,
            resolverContext.ScopedContextData,
            path,
            parent);

        return parentResult;
    }

    private static void ResolveAndCompleteInline(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        int responseIndex,
        ObjectType parentType,
        object parent,
        ObjectResult parentResult,
        List<ResolverTask> bufferedTasks)
    {
        var committed = false;
        object? resolverResult = null;

        try
        {
            if (TryExecute(out resolverResult))
            {
                committed = true;

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
        }
        catch (OperationCanceledException)
        {
            // If we run into this exception the request was aborted.
            // In this case we do nothing and just return.
        }
        catch (Exception ex)
        {
            if (operationContext.RequestAborted.IsCancellationRequested)
            {
                // if cancellation is request we do no longer report errors to the
                // operation context.
                return;
            }

            ValueCompletion.ReportError(
                operationContext,
                resolverContext,
                selection,
                path,
                ex);
        }

        if (!committed)
        {
            CommitValue(
                operationContext,
                selection,
                path,
                responseIndex,
                parentResult,
                resolverResult);
        }

        bool TryExecute(out object? result)
        {
            try
            {
                if (resolverContext.TryCreatePureContext(
                    selection, path, parentType, parent,
                    out var childContext))
                {
                    result = selection.PureResolver!(childContext);
                    return true;
                }
            }
            catch (Exception ex)
            {
                ValueCompletion.ReportError(
                    operationContext,
                    resolverContext,
                    selection,
                    path,
                    ex);
            }

            result = null;
            return false;
        }
    }

    private static void CompleteInline(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        IType elementType,
        Path path,
        int responseIndex,
        ObjectResult resultMap,
        object? value,
        List<ResolverTask> bufferedTasks)
    {
        object? completedValue = null;

        try
        {
            if (ValueCompletion.TryComplete(
                operationContext,
                resolverContext,
                selection,
                path,
                elementType,
                selection.ResponseName,
                responseIndex,
                value,
                bufferedTasks,
                out completedValue) &&
                elementType.Kind is not TypeKind.Scalar and not TypeKind.Enum &&
                completedValue is ResultData result)
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
            if (operationContext.RequestAborted.IsCancellationRequested)
            {
                // if cancellation is request we do no longer report errors to the
                // operation context.
                return;
            }

            ValueCompletion.ReportError(
                operationContext,
                resolverContext,
                selection,
                path,
                ex);
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
        IOperationContext operationContext,
        ISelection selection,
        Path path,
        int responseIndex,
        ObjectResult parentResult,
        object? completedValue)
    {
        var isNonNullType = selection.Type.Kind is TypeKind.NonNull;

        if (completedValue is null && isNonNullType)
        {
            // if we detect a non-null violation we will stash it for later.
            // the non-null propagation is delayed so that we can parallelize better.
            operationContext.Result.AddNonNullViolation(
                selection.SyntaxNode,
                path,
                parentResult);
        }
        else
        {
            parentResult.SetValueUnsafe(
                responseIndex,
                selection.ResponseName,
                completedValue,
                !isNonNullType);
        }
    }

    private static ResolverTask CreateResolverTask(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        int responseIndex,
        object parent,
        ObjectResult parentResult)
    {
        var task = operationContext.ResolverTasks.Get();

        task.Initialize(
            operationContext,
            selection,
            parentResult,
            responseIndex,
            parent,
            path,
            resolverContext.ScopedContextData);

        return task;
    }

    private static ResolverTask CreateResolverTask(
        IOperationContext operationContext,
        ISelection selection,
        object? parent,
        int responseIndex,
        Path path,
        ObjectResult result,
        IImmutableDictionary<string, object?> scopedContext)
    {
        var task = operationContext.ResolverTasks.Get();

        task.Initialize(
            operationContext,
            selection,
            result,
            responseIndex,
            parent,
            path,
            scopedContext);

        return task;
    }

    private static void TryHandleDeferredFragments(
        IOperationContext operationContext,
        ISelectionSet selectionSet,
        IImmutableDictionary<string, object?> scopedContext,
        Path path,
        object? parent)
    {
        if (selectionSet.Fragments.Count > 0)
        {
            var fragments = selectionSet.Fragments;
            var includeFlags = operationContext.IncludeFlags;

            for (var i = 0; i < fragments.Count; i++)
            {
                var fragment = fragments[i];
                if (!fragment.IsConditional || fragment.IsIncluded(includeFlags))
                {
                    operationContext.Scheduler.DeferredWork.Register(
                        new DeferredFragment(
                            fragment,
                            fragment.GetLabel(operationContext.Variables),
                            path,
                            parent,
                            scopedContext));
                }
            }
        }
    }

    private sealed class NoOpExecutionTask : ExecutionTask
    {
        public NoOpExecutionTask(IOperationContext context)
        {
            Context = (IExecutionTaskContext)context;
        }

        protected override IExecutionTaskContext Context { get; }

        protected override ValueTask ExecuteAsync(CancellationToken cancellationToken)
            => default;
    }
}
