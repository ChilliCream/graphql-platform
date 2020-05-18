using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal static class ValueCompletion
    {
        public static object? Complete(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            IType fieldType,
            object? result)
        {
            throw new NotImplementedException();
        }

        private static object? CompleteInternal(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            IType fieldType,
            object? result,
            ref bool nonNullViolation)
        {
            if (fieldType.IsNonNullType())
            {
                object? completedResult = Complete(
                    operationContext,
                    middlewareContext,
                    path,
                    fieldType.InnerType(),
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

        private static IResultData CompleteListValue(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            IType fieldType,
            object? result)
        {
            IType elementType = fieldType.ElementType();
            if (elementType.IsCompositeType())
            {
                return CompleteResultMapListValue(
                    operationContext,
                    middlewareContext,
                    path,
                    fieldType,
                    elementType,
                    result);
            }

            return CompleteResultListValue(
                operationContext,
                middlewareContext,
                path,
                fieldType,
                elementType,
                result);
        }

        private static ResultMapList CompleteResultMapListValue(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            IType fieldType,
            IType elementType,
            object? result)
        {
            if (result is Array array)
            {
                ResultMapList completedResult = operationContext.Result.RentResultMapList();
                completedResult.IsNullable = elementType.IsNullableType();

                for (int i = 0; i < array.Length; i++)
                {
                    completedResult.Add((ResultMap)Complete(
                        operationContext,
                        middlewareContext,
                        path.Append(i),
                        elementType,
                        array.GetValue(i))!);
                }

                return completedResult;
            }
            else if (result is IList list)
            {
                ResultMapList completedResult = operationContext.Result.RentResultMapList();
                completedResult.IsNullable = elementType.IsNullableType();

                for (int i = 0; i < list.Count; i++)
                {
                    completedResult.Add((ResultMap)Complete(
                        operationContext,
                        middlewareContext,
                        path.Append(i),
                        elementType,
                        list[i])!);
                }

                return completedResult;
            }
            else if (result is IEnumerable enumerable)
            {
                int index = 0;
                ResultMapList completedResult = operationContext.Result.RentResultMapList();
                completedResult.IsNullable = elementType.IsNullableType();

                foreach (object? element in enumerable)
                {
                    completedResult.Add((ResultMap)Complete(
                        operationContext,
                        middlewareContext,
                        path.Append(index++),
                        elementType,
                        element)!);
                }

                return completedResult;
            }
            else
            {
                // TODO : error helper
                throw new GraphQLException("not a list error");
            }
        }

        private static ResultList CompleteResultListValue(
           IOperationContext operationContext,
           IMiddlewareContext middlewareContext,
           Path path,
           IType fieldType,
           IType elementType,
           object? result)
        {
            if (result is Array array)
            {
                ResultList completedResult = operationContext.Result.RentResultList();
                completedResult.IsNullable = elementType.IsNullableType();

                for (int i = 0; i < array.Length; i++)
                {
                    completedResult.Add(Complete(
                        operationContext,
                        middlewareContext,
                        path.Append(i),
                        elementType,
                        array.GetValue(i)));
                }

                return completedResult;
            }
            else if (result is IList list)
            {
                ResultList completedResult = operationContext.Result.RentResultList();
                completedResult.IsNullable = elementType.IsNullableType();

                for (int i = 0; i < list.Count; i++)
                {
                    completedResult.Add(Complete(
                        operationContext,
                        middlewareContext,
                        path.Append(i),
                        elementType,
                        list[i]));
                }

                return completedResult;
            }
            else if (result is IEnumerable enumerable)
            {
                int index = 0;
                ResultList completedResult = operationContext.Result.RentResultList();
                completedResult.IsNullable = elementType.IsNullableType();

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
            IPreparedSelectionList selections = operationContext.CollectFields(
                selectionSet, objectType);
            
            ResultMap resultMap = selections.EnqueueResolverTasks(
                operationContext, 
                n => middlewareContext.Path.Append(n), 
                middlewareContext.ScopedContextData);

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
