using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

            for (var i = 0; i < selections.Count; i++)
            {
                ISelection selection = selections[i];
                if (selection.IsIncluded(operationContext.Variables))
                {
                    backlog.Register(CreateResolverTask(
                        operationContext,
                        selection,
                        parent,
                        responseIndex++,
                        path.Append(selection.ResponseName),
                        resultMap,
                        scopedContext));
                }
            }

            TryHandleDeferredFragments(
                operationContext,
                selectionSet,
                scopedContext,
                path,
                parent);

            return resultMap;
        }

        public static ResultMap EnqueueOrInlineResolverTasks(
            ref ValueCompletionContext context,
            ISelectionSet selectionSet)
        {
            var responseIndex = 0;
            IReadOnlyList<ISelection> selections = selectionSet.Selections;
            IWorkBacklog backlog = context.OperationContext.Execution.Work;
            ResultMap resultMap = context.OperationContext.Result.RentResultMap(selections.Count);
            IVariableValueCollection variables = context.OperationContext.Variables;
            ValueCompletionContext selectionContext = context.Copy();

            object result = context.Result!;
            Path path = context.Path;

            for (var i = 0; i < selections.Count; i++)
            {
                ISelection selection = selections[i];

                if (selection.IsIncluded(variables))
                {
                    selectionContext.Selection = selection;
                    selectionContext.Path = path.Append(selection.ResponseName);
                    selectionContext.FieldType = selection.Field.Type;
                    selectionContext.ResponseName = selection.ResponseName;
                    selectionContext.ResponseIndex = responseIndex++;
                    selectionContext.Result = result;

                    switch (selection.Strategy)
                    {
                        case SelectionExecutionStrategy.Inline:
                            ResolveAndCompleteInline(ref selectionContext, resultMap);
                            break;

                        case SelectionExecutionStrategy.Pure:
                            backlog.Register(CreatePureResolverTask(ref selectionContext, resultMap));
                            break;

                        // parallel and serial are always enqueued.
                        default:
                            backlog.Register(CreateResolverTask(ref selectionContext, resultMap));
                            break;
                    }
                }
            }

            TryHandleDeferredFragments(
                context.OperationContext,
                selectionSet,
                context.ResolverContext.ScopedContextData,
                path,
                result);

            return resultMap;
        }

        private static void ResolveAndCompleteInline(
            ref ValueCompletionContext context,
            ResultMap resultMap)
        {

            ISelection selection = context.Selection;
            object? completedValue = null;

            try
            {
                context.Result = selection.InlineResolver!(context.Result);

                if (ValueCompletion2.TryComplete(ref context, out completedValue) &&
                    context.FieldType.Kind is not TypeKind.Scalar and not TypeKind.Enum &&
                    completedValue is IHasResultDataParent result)
                {
                    result.Parent = resultMap;
                }

                ValueCompletion2.TryComplete(ref context, out completedValue);
            }
            catch (OperationCanceledException)
            {
                // If we run into this exception the request was aborted.
                // In this case we do nothing and just return.
                return;
            }
            catch (Exception ex)
            {
                if (context.OperationContext.RequestAborted.IsCancellationRequested)
                {
                    // if cancellation is request we do no longer report errors to the
                    // operation context.
                    return;
                }

                context.ReportError(ex);
                context.Result = null;
            }

            if (completedValue is null && selection.Field.Type.IsNonNullType())
            {
                // if we detect a non-null violation we will stash it for later.
                // the non-null propagation is delayed so that we can parallelize better.
                context.OperationContext.Result.AddNonNullViolation(
                    selection.SyntaxNode,
                    context.Path,
                    resultMap);
            }
            else
            {
                resultMap.SetValue(
                    context.ResponseIndex,
                    context.ResponseName,
                    completedValue,
                    context.FieldType.IsNullableType());
            }
        }

        private static IExecutionTask CreateResolverTask(
            ref ValueCompletionContext context,
            ResultMap resultMap)
        {
            ResolverTask task = context.OperationContext.Execution.ResolverTasks.Get();

            task.Initialize(
                context.OperationContext,
                context.Selection,
                resultMap,
                context.ResponseIndex,
                context.Result,
                context.Path,
                context.ResolverContext.ScopedContextData);

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

        private static IExecutionTask CreatePureResolverTask(
            ref ValueCompletionContext context,
            ResultMap resultMap)
        {
            PureResolverTask task = context.OperationContext.Execution.PureResolverTasks.Get();

            task.Initialize(
                context.OperationContext,
                context.Selection,
                resultMap,
                context.ResponseIndex,
                context.Result,
                context.Path,
                context.ResolverContext.ScopedContextData);

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
