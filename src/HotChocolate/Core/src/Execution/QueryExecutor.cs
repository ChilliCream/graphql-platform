using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class QueryExecutor : IOperationExecutor
    {
        public Task<IExecutionResult> ExecuteAsync(
            IOperationContext executionContext,
            CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        private async Task ExecuteResolversAsync(
            IOperationContext operationContext,
            IBatchScheduler batchScheduler, // we might have more than one
            ITaskQueue taskQueue
        )
        {
            while (taskQueue.HasTasks)
            {
                while (taskQueue.HasTasks)
                {
                    MiddlewareContext middlewareContext = taskQueue.Dequeue();
                    FieldDelegate resolverPipeline = middlewareContext.ResolverPipeline;
                    middlewareContext.Task = resolverPipeline(middlewareContext);
                }

                while (taskQueue.IsEmpty && batchScheduler.HasTasks)
                {
                    await batchScheduler.DispatchAsync(
                        operationContext.RequestAborted)
                        .ConfigureAwait(false);
                }
            }
        }

    }

    internal interface ITaskQueue
    {
        bool HasTasks { get; }

        bool IsEmpty { get; }

        MiddlewareContext Dequeue();
    }

    internal interface IBatchScheduler
    {
        bool HasTasks { get; }

        Task DispatchAsync(CancellationToken cancellationToken);
    }

    internal static class ValueCompletion
    {
        public static object? Complete(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            IType fieldType,
            object? result)
        {
            if (fieldType.IsNonNullType())
            {
                object? completedResult = Complete(
                    operationContext,
                    middlewareContext,
                    path,
                    fieldType,
                    result);

                if (completedResult is null)
                {
                    // TODO : error helper
                    throw new GraphQLException("non-null error");
                }

                return completedResult;
            }

            if (result is null)
            {
                return null;
            }

            if (fieldType.IsListType())
            {
                CompleteListValue(
                    operationContext,
                    middlewareContext,
                    path,
                    fieldType,
                    result);
            }

            if (fieldType is ILeafType leafType)
            {
                CompleteLeafValue(
                    operationContext,
                    middlewareContext,
                    path,
                    leafType,
                    result);
            }

            if (fieldType.IsCompositeType())
            {
                CompleteCompositeValue(
                    operationContext,
                    middlewareContext,
                    path,
                    fieldType,
                    result);
            }

            // TODO : error helper
            throw new GraphQLException("unexpected");
        }

        private static object? CompleteListValue(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            IType fieldType,
            object? result)
        {
            IType elementType = fieldType.ElementType();

            if (result is Array array)
            {
                var completedResult = new object?[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    completedResult[i] = Complete(
                        operationContext,
                        middlewareContext,
                        path.Append(i),
                        elementType,
                        array.GetValue(i));
                }
                return completedResult;
            }
            else if (result is IList list)
            {
                var completedResult = new object?[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    completedResult[i] = Complete(
                        operationContext,
                        middlewareContext,
                        path.Append(i),
                        elementType,
                        list[i]);
                }
                return completedResult;
            }
            else if (result is IEnumerable enumerable)
            {
                int index = 0;
                var completedResult = new List<object?>();

                foreach (object? element in enumerable)
                {
                    completedResult.Add(Complete(
                        operationContext,
                        middlewareContext,
                        path.Append(index++),
                        elementType,
                        element));
                }
                return completedResult;
            }
            else
            {
                // TODO : error helper
                throw new GraphQLException("not a list error");
            }
        }

        private static object? CompleteLeafValue(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            ILeafType fieldType,
            object? result)
        {
            try
            {
                return fieldType.Serialize(result);
            }
            catch (ScalarSerializationException ex)
            {
                // TODO : error helper
                throw new GraphQLException("not a list error");
            }
            catch (Exception ex)
            {
                // TODO : error helper
                throw new GraphQLException("not a list error");
            }
        }

        private static object? CompleteCompositeValue(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            IType fieldType,
            object result)
        {
            ObjectType objectType = ResolveObjectType(middlewareContext, fieldType, result);
            SelectionSetNode selectionSet = middlewareContext.FieldSelection.SelectionSet!;
            IReadOnlyList<IPreparedSelection> selections =
                operationContext.CollectFields(selectionSet, objectType);
            ResultMap resultMap = operationContext.RentResultMap(selections.Count);
            IImmutableStack<object?> source = middlewareContext.Source.Push(result);

            for (int i = 0; i < selections.Count; i++)
            {
                IPreparedSelection selection = selections[i];

                operationContext.RentMiddlewareContext(
                    selection,
                    result,
                    source,
                    middlewareContext.Path.Append(selection.ResponseName),
                    middlewareContext.ScopedContextData);
            }

            return resultMap;
        }

        private static ObjectType ResolveObjectType(
            IMiddlewareContext middlewareContext,
            IType fieldType,
            object result)
        {
            if (middlewareContext.ValueType is null &&
                middlewareContext.ValueType is ObjectType objectType)
            {
                return objectType;
            }
            else if (fieldType is ObjectType ot)
            {
                return ot;
            }
            else if (fieldType is InterfaceType it)
            {
                return it.ResolveType(middlewareContext, result);
            }
            else if (fieldType is UnionType ut)
            {
                return ut.ResolveType(middlewareContext, result);
            }

            // TODO : throw helper
            throw new NotSupportedException(
                "CoreResources.ResolveObjectType_TypeNotSupported");
        }
    }
}
