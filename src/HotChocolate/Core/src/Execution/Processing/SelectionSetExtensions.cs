using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    internal static class SelectionSetExtensions
    {
        public static ResultMap EnqueueResolverTasks(
            this ISelectionSet selectionSet,
            IOperationContext operationContext,
            Path path,
            IImmutableDictionary<string, object?> scopedContext,
            object? parent)
        {
            var responseIndex = 0;
            IReadOnlyList<ISelection> selections = selectionSet.Selections;
            ResultMap resultMap = operationContext.Result.RentResultMap(selections.Count);

            BatchExecutionTask? batchExecutionTask = null;
            IExecutionTask? single = null;

            for (var i = 0; i < selections.Count; i++)
            {
                ISelection selection = selections[i];
                if (selection.IsIncluded(operationContext.Variables))
                {
                    IExecutionTask task = operationContext.CreateTask(
                        new ResolverTaskDefinition(
                            operationContext,
                            selection,
                            responseIndex++,
                            resultMap,
                            parent,
                            path.Append(selection.ResponseName),
                            scopedContext));

                    if (batchExecutionTask is not null)
                    {
                        batchExecutionTask.AddExecutionTask(task);
                    }
                    else if (single is null)
                    {
                        single = task;
                    }
                    else
                    {
                        batchExecutionTask = operationContext.CreateBatchTask();
                        batchExecutionTask.AddExecutionTask(single);
                        batchExecutionTask.AddExecutionTask(task);
                        single = null;
                    }
                }
            }

            if (single is not null || batchExecutionTask is not null)
            {
                operationContext.Execution.Work.Register(single ?? batchExecutionTask!);
            }

            TryHandleDeferredFragments(
                operationContext,
                selectionSet,
                scopedContext,
                path,
                parent);

            return resultMap;
        }

        public static ResultMap EnqueueResolverTasks(
            this ISelectionSet selectionSet,
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            object? parent,
            bool allowInlining = false)
        {
            var responseIndex = -1;
            IReadOnlyList<ISelection> selections = selectionSet.Selections;
            ResultMap resultMap = operationContext.Result.RentResultMap(selections.Count);

            BatchExecutionTask? batchExecutionTask = null;
            IExecutionTask? single = null;

            for (var i = 0; i < selections.Count; i++)
            {
                ISelection selection = selections[i];

                if (selection.IsIncluded(operationContext.Variables))
                {
                    responseIndex++;

                    if (allowInlining &&
                        TryResolveAndCompleteInline(
                            operationContext,
                            middlewareContext,
                            resultMap,
                            parent,
                            selection,
                            responseIndex,
                            path.Append(selection.ResponseName)))
                    {
                        continue;
                    }

                    IExecutionTask task = operationContext.CreateTask(
                        new ResolverTaskDefinition(
                            operationContext,
                            selection,
                            responseIndex,
                            resultMap,
                            parent,
                            path.Append(selection.ResponseName),
                            middlewareContext.ScopedContextData));

                    if (batchExecutionTask is not null)
                    {
                        batchExecutionTask.AddExecutionTask(task);
                    }
                    else if (single is null)
                    {
                        single = task;
                    }
                    else
                    {
                        batchExecutionTask = operationContext.CreateBatchTask();
                        batchExecutionTask.AddExecutionTask(single);
                        batchExecutionTask.AddExecutionTask(task);
                        single = null;
                    }
                }
            }

            if (single is not null || batchExecutionTask is not null)
            {
                operationContext.Execution.Work.Register(single ?? batchExecutionTask!);
            }

            TryHandleDeferredFragments(
                operationContext,
                selectionSet,
                middlewareContext.ScopedContextData,
                path,
                parent);

            return resultMap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryHandleDeferredFragments(
            IOperationContext operationContext,
            ISelectionSet selectionSet,
            IImmutableDictionary<string, object?> scopedContext,
            Path path,
            object? parent)
        {
            if (selectionSet.Fragments.Count > 0)
            {
                IReadOnlyList<IFragment> fragments = selectionSet.Fragments;
                for (var i = 0; i < fragments.Count; i++)
                {
                    IFragment fragment = fragments[i];
                    if (!fragment.IsConditional)
                    {
                        operationContext.Execution.DeferredWork.Register(
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

        private static bool TryResolveAndCompleteInline(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            ResultMap resultMap,
            object? parent,
            ISelection selection,
            int responseIndex,
            Path path)
        {
            if (selection.Field.Type.NamedType().IsLeafType() &&
                selection.InlineResolver is not null)
            {
                object? completedValue = null;

                try
                {
                    var value = selection.InlineResolver(parent);

                    ValueCompletion.TryComplete(
                        operationContext,
                        middlewareContext,
                        selection,
                        path.Append(selection.ResponseName),
                        selection.Field.Type,
                        value,
                        out completedValue);
                }
                catch (OperationCanceledException)
                {
                    // If we run into this exception the request was aborted.
                    // In this case we do nothing and just return.
                    return false;
                }
                catch(Exception ex)
                {
                    if (operationContext.RequestAborted.IsCancellationRequested)
                    {
                        // if cancellation is request we do no longer report errors to the
                        // operation context.
                        return false;
                    }

                    ReportError(operationContext, middlewareContext, selection, path, ex);
                }

                if (completedValue is null && selection.Field.Type.IsNonNullType())
                {
                    // if we detect a non-null violation we will stash it for later.
                    // the non-null propagation is delayed so that we can parallelize better.
                    operationContext.Result.AddNonNullViolation(
                        selection.SyntaxNode,
                        path,
                        resultMap);
                }
                else
                {
                    resultMap.SetValue(
                        responseIndex,
                        selection.ResponseName,
                        completedValue,
                        selection.Field.Type.IsNullableType());
                }

                return true;
            }

            return false;
        }

        private static void ReportError(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            ISelection selection,
            Path path,
            Exception exception)
        {
            if (exception is GraphQLException graphQLException)
            {
                foreach (IError error in graphQLException.Errors)
                {
                    ReportError(operationContext, middlewareContext, selection, error);
                }
            }
            else
            {
                IError error = operationContext.ErrorHandler
                    .CreateUnexpectedError(exception)
                    .SetPath(path)
                    .AddLocation(selection.SyntaxNode)
                    .Build();

                ReportError(operationContext, middlewareContext, selection, error);
            }
        }

        private static void ReportError(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            ISelection selection,
            IError error)
        {
            error = operationContext.ErrorHandler.Handle(error);
            operationContext.Result.AddError(error, selection.SyntaxNode);
            operationContext.DiagnosticEvents.ResolverError(middlewareContext, error);
        }
    }
}
