using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal static class ResolverTaskFactory
    {
        public static ResultMap EnqueueResolverTasks(
            IOperationContext operationContext,
            ISelectionSet selectionSet,
            object? parent,
            Path path,
            IImmutableDictionary<string, object?> scopedContext)
        {
            var responseIndex = 0;
            IReadOnlyList<ISelection> selections = selectionSet.Selections;
            ResultMap resultMap = operationContext.Result.RentResultMap(selections.Count);
            IWorkBacklog backlog = operationContext.Execution.Work;
            IExecutionTask?[] buffer = operationContext.Execution.TaskBuffers.Get();
            var bufferSize = buffer.Length;
            var buffered = 0;

            try
            {
                for (var i = 0; i < selections.Count; i++)
                {
                    ISelection selection = selections[i];
                    if (selection.IsIncluded(operationContext.Variables))
                    {
                        if (buffered == bufferSize)
                        {
                            backlog.Register(buffer, bufferSize);
                            buffered = 0;
                        }

                        buffer[buffered++] = CreateResolverTask(
                            operationContext,
                            selection,
                            parent,
                            responseIndex++,
                            path.Append(selection.ResponseName),
                            resultMap,
                            scopedContext);
                    }
                }

                if (buffered > 0)
                {
                    backlog.Register(buffer, buffered);
                }

                TryHandleDeferredFragments(
                    operationContext,
                    selectionSet,
                    scopedContext,
                    path,
                    parent);

                return resultMap;
            }
            finally
            {
                operationContext.Execution.TaskBuffers.Return(buffer);
            }
        }

        public static ResultMap EnqueueOrInlineResolverTasks(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            Path path,
            object result,
            ISelectionSet selectionSet)
        {
            var responseIndex = 0;
            IReadOnlyList<ISelection> selections = selectionSet.Selections;
            IWorkBacklog backlog = operationContext.Execution.Work;
            ResultMap resultMap = operationContext.Result.RentResultMap(selections.Count);
            IVariableValueCollection variables = operationContext.Variables;
            IExecutionTask?[] buffer = operationContext.Execution.TaskBuffers.Get();
            var bufferSize = buffer.Length;
            var buffered = 0;

            try
            {
                for (var i = 0; i < selections.Count; i++)
                {
                    ISelection selection = selections[i];

                    if (selection.IsIncluded(variables))
                    {
                        if (buffered == bufferSize)
                        {
                            backlog.Register(buffer, buffer.Length);
                            buffered = 0;
                        }

                        if (selection.Strategy is SelectionExecutionStrategy.Pure)
                        {
                            ResolveAndCompleteInline(
                                operationContext,
                                resolverContext,
                                selection,
                                path.Append(selection.ResponseName),
                                responseIndex++,
                                result,
                                resultMap);
                        }
                        else
                        {
                            buffer[buffered++] = CreateResolverTask(
                                operationContext,
                                resolverContext,
                                selection,
                                path.Append(selection.ResponseName),
                                responseIndex++,
                                result,
                                resultMap);
                        }
                    }
                }

                if (buffered > 0)
                {
                    backlog.Register(buffer, buffered);
                }

                TryHandleDeferredFragments(
                    operationContext,
                    selectionSet,
                    resolverContext.ScopedContextData,
                    path,
                    result);

                return resultMap;
            }
            finally
            {
                operationContext.Execution.TaskBuffers.Return(buffer);
            }
        }

        private static void ResolveAndCompleteInline(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path,
            int responseIndex,
            object parent,
            ResultMap resultMap)
        {
            object? completedValue = null;

            try
            {
                if (TryExecute(out var resolverResult) &&
                    ValueCompletion.TryComplete(
                        operationContext,
                        resolverContext,
                        selection,
                        path,
                        selection.Type,
                        selection.ResponseName,
                        responseIndex,
                        resolverResult,
                        out completedValue) &&
                    selection.TypeKind is not TypeKind.Scalar and not TypeKind.Enum &&
                    completedValue is IHasResultDataParent result)
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
                    selection.Type.IsNullableType());
            }

            bool TryExecute(out object? result)
            {
                try
                {
                    if (resolverContext.TryCreatePureContext(
                        selection, path, parent,
                        out IPureResolverContext? childContext))
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

        private static IExecutionTask CreateResolverTask(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path,
            int responseIndex,
            object parent,
            ResultMap resultMap)
        {
            ResolverTask task = operationContext.Execution.ResolverTasks.Get();

            task.Initialize(
                operationContext,
                selection,
                resultMap,
                responseIndex,
                parent,
                path,
                resolverContext.ScopedContextData);

            return task;
        }

        public static IExecutionTask CreateResolverTask(
            IOperationContext operationContext,
            ISelection selection,
            object? parent,
            int responseIndex,
            Path path,
            ResultMap resultMap,
            IImmutableDictionary<string, object?> scopedContext)
        {
            ResolverTask task = operationContext.Execution.ResolverTasks.Get();

            task.Initialize(
                operationContext,
                selection,
                resultMap,
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
    }
}
