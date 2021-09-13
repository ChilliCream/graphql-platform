using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal static class ResolverTaskFactory
    {
        private static List<IExecutionTask>? _pooled = new();

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
            IWorkScheduler scheduler = operationContext.Scheduler;
            List<IExecutionTask> bufferedTasks = Interlocked.Exchange(ref _pooled, null) ?? new();
            var final = !selectionSet.IsConditional;

            try
            {
                for (var i = 0; i < selections.Count; i++)
                {
                    ISelection selection = selections[i];
                    if (final || selection.IsIncluded(operationContext.Variables))
                    {
                        bufferedTasks.Add(CreateResolverTask(
                            operationContext,
                            selection,
                            parent,
                            responseIndex++,
                            path.Append(selection.ResponseName),
                            resultMap,
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

                return resultMap;
            }
            finally
            {
                bufferedTasks.Clear();
                Interlocked.Exchange(ref _pooled, bufferedTasks);
            }
        }

        public static async ValueTask<ResultMap?> ExecuteElementAsync(
            IOperationContext operationContext,
            ISelection selection,
            object? parent,
            Path path,
            int index,
            IAsyncEnumerator<object?> value,
            IImmutableDictionary<string, object?> scopedContext)
        {
            if (await value.MoveNextAsync())
            {
                ResultMap resultMap = operationContext.Result.RentResultMap(1);

                List<IExecutionTask> bufferedTasks =
                    Interlocked.Exchange(ref _pooled, null) ??
                    new List<IExecutionTask>();

                ResolverTask resolverTask = CreateResolverTask(
                    operationContext,
                    selection,
                    parent,
                    0,
                    path,
                    resultMap,
                    scopedContext);

                try
                {
                    CompleteInline(
                        operationContext,
                        resolverTask.ResolverContext,
                        selection,
                        selection.Type.ElementType(),
                        path.Append(index),
                        0,
                        resultMap,
                        value.Current,
                        bufferedTasks);
                }
                finally
                {
                    Interlocked.Exchange(ref _pooled, bufferedTasks);
                }

                return resultMap;
            }


            return null;
        }

        public static ResultMap EnqueueOrInlineResolverTasks(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            Path path,
            ObjectType resultType,
            object result,
            ISelectionSet selectionSet,
            List<IExecutionTask> bufferedTasks)
        {
            var responseIndex = 0;
            IReadOnlyList<ISelection> selections = selectionSet.Selections;
            ResultMap resultMap = operationContext.Result.RentResultMap(selections.Count);
            IVariableValueCollection variables = operationContext.Variables;
            var final = !selectionSet.IsConditional;

            for (var i = 0; i < selections.Count; i++)
            {
                ISelection selection = selections[i];

                if (final || selection.IsIncluded(variables))
                {
                    if (selection.Strategy is SelectionExecutionStrategy.Pure)
                    {
                        ResolveAndCompleteInline(
                            operationContext,
                            resolverContext,
                            selection,
                            path.Append(selection.ResponseName),
                            responseIndex++,
                            resultType,
                            result,
                            resultMap,
                            bufferedTasks);
                    }
                    else
                    {
                        bufferedTasks.Add(CreateResolverTask(
                            operationContext,
                            resolverContext,
                            selection,
                            path.Append(selection.ResponseName),
                            responseIndex++,
                            result,
                            resultMap));
                    }
                }
            }

            TryHandleDeferredFragments(
                operationContext,
                selectionSet,
                resolverContext.ScopedContextData,
                path,
                result);

            return resultMap;
        }

        private static void ResolveAndCompleteInline(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path,
            int responseIndex,
            ObjectType parentType,
            object parent,
            ResultMap resultMap,
            List<IExecutionTask> bufferedTasks)
        {
            try
            {
                if (TryExecute(out var resolverResult))
                {
                    CompleteInline(
                        operationContext,
                        resolverContext,
                        selection,
                        selection.Type,
                        path,
                        responseIndex,
                        resultMap,
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

            bool TryExecute(out object? result)
            {
                try
                {
                    if (resolverContext.TryCreatePureContext(
                        selection, path, parentType, parent,
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

        private static void CompleteInline(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            IType elementType,
            Path path,
            int responseIndex,
            ResultMap resultMap,
            object? value,
            List<IExecutionTask> bufferedTasks)
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

            var isNullable = selection.Type.IsNonNullType();

            if (completedValue is null && isNullable)
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
                    isNullable);
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
            ResolverTask task = operationContext.ResolverTasks.Get();

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

        private static ResolverTask CreateResolverTask(
            IOperationContext operationContext,
            ISelection selection,
            object? parent,
            int responseIndex,
            Path path,
            ResultMap resultMap,
            IImmutableDictionary<string, object?> scopedContext)
        {
            ResolverTask task = operationContext.ResolverTasks.Get();

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

        private sealed class NoOpExecutionTask : ParallelExecutionTask
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
}
