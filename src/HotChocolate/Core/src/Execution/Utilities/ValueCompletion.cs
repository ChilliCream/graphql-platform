using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.Utilities.ErrorHelper;

namespace HotChocolate.Execution.Utilities
{
    internal static class ValueCompletion
    {
        public static bool TryComplete(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            IType fieldType,
            object? result,
            out object? completedResult)
        {
            if (fieldType.IsNonNullType())
            {
                if (TryComplete(
                    operationContext,
                    middlewareContext,
                    path,
                    fieldType.InnerType(),
                    result,
                    out completedResult) &&
                    completedResult is { })
                {
                    return true;
                }

                return false;
            }

            if (result is null)
            {
                completedResult = null;
                return true;
            }

            if (fieldType.IsListType())
            {
                if (TryCompleteListValue(
                    operationContext,
                    middlewareContext,
                    path,
                    fieldType,
                    result,
                    out IResultData? completedList))
                {
                    completedResult = completedList;
                    return true;
                }

                completedResult = null;
                return false;
            }

            if (fieldType is ILeafType leafType)
            {
                return TryCompleteLeafValue(
                    operationContext,
                    middlewareContext,
                    path,
                    leafType,
                    result,
                    out completedResult);
            }

            if (fieldType.IsCompositeType())
            {
                if (TryCompleteCompositeValue(
                    operationContext,
                    middlewareContext,
                    path,
                    fieldType,
                    result,
                    out ResultMap? completedResultMap))
                {
                    completedResult = completedResultMap;
                    return true;
                }

                completedResult = null;
                return false;
            }

            middlewareContext.ReportError(
                UnexpectedValueCompletionError(
                    middlewareContext.FieldSelection,
                    path));

            completedResult = null;
            return false;
        }

        private static bool TryCompleteListValue(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            IType fieldType,
            object result,
            out IResultData? completedValue)
        {
            IType elementType = fieldType.ElementType();

            if (elementType.IsCompositeType())
            {
                if (TryCompleteResultMapListValue(
                    operationContext,
                    middlewareContext,
                    path,
                    elementType,
                    result,
                    out ResultMapList? mapList))
                {
                    completedValue = mapList;
                    return true;
                }
            }
            else if (TryCompleteResultListValue(
                operationContext,
                middlewareContext,
                path,
                elementType,
                result,
                out ResultList? list))
            {
                completedValue = list;
                return true;
            }

            completedValue = null;
            return false;
        }

        private static bool TryCompleteResultMapListValue(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            IType elementType,
            object result,
            out ResultMapList? completedResult)
        {
            if (result is Array array)
            {
                completedResult = operationContext.Result.RentResultMapList();
                completedResult.IsNullable = elementType.IsNullableType();

                for (var i = 0; i < array.Length; i++)
                {
                    if (TryComplete(
                        operationContext,
                        middlewareContext,
                        path.Append(i),
                        elementType,
                        array.GetValue(i),
                        out object? completedElement) &&
                        completedElement is ResultMap m)
                    {
                        m.Parent = completedResult;
                        completedResult.Add(m);
                    }
                    else if (completedResult.IsNullable)
                    {
                        completedResult.Add(null);
                    }
                    else
                    {
                        completedResult = null;
                        return false;
                    }
                }

                return true;
            }

            if (result is IList list)
            {
                completedResult = operationContext.Result.RentResultMapList();
                completedResult.IsNullable = elementType.IsNullableType();

                for (var i = 0; i < list.Count; i++)
                {
                    if (TryComplete(
                            operationContext,
                            middlewareContext,
                            path.Append(i),
                            elementType,
                            list[i],
                            out object? completedElement) &&
                        completedElement is ResultMap m)
                    {
                        m.Parent = completedResult;
                        completedResult.Add(m);
                    }
                    else if (completedResult.IsNullable)
                    {
                        completedResult.Add(null);
                    }
                    else
                    {
                        completedResult = null;
                        return false;
                    }
                }

                return true;
            }

            if (result is IEnumerable enumerable)
            {
                var index = 0;
                completedResult = operationContext.Result.RentResultMapList();
                completedResult.IsNullable = elementType.IsNullableType();

                foreach (object? element in enumerable)
                {
                    if (TryComplete(
                            operationContext,
                            middlewareContext,
                            path.Append(index++),
                            elementType,
                            element,
                            out object? completedElement) &&
                        completedElement is ResultMap m)
                    {
                        m.Parent = completedResult;
                        completedResult.Add(m);
                    }
                    else if (completedResult.IsNullable)
                    {
                        completedResult.Add(null);
                    }
                    else
                    {
                        completedResult = null;
                        return false;
                    }
                }

                return true;
            }

            middlewareContext.ReportError(
                ListValueIsNotSupported(
                    result.GetType(),
                    middlewareContext.FieldSelection,
                    path));
            completedResult = null;
            return false;
        }

        private static bool TryCompleteResultListValue(
           IOperationContext operationContext,
           IMiddlewareContext middlewareContext,
           Path path,
           IType elementType,
           object result,
           out ResultList? completedResult)
        {
            var isElementList = elementType.IsListType();

            if (result is Array array)
            {
                completedResult = operationContext.Result.RentResultList();
                completedResult.IsNullable = elementType.IsNullableType();

                for (var i = 0; i < array.Length; i++)
                {
                    if (TryComplete(
                        operationContext,
                        middlewareContext,
                        path.Append(i),
                        elementType,
                        array.GetValue(i),
                        out object? completedElement) &&
                        completedElement is { } m)
                    {
                        completedResult.Add(m);
                        if (isElementList)
                        {
                            SetParent(m, completedResult);
                        }
                    }
                    else if (completedResult.IsNullable)
                    {
                        completedResult.Add(null);
                    }
                    else
                    {
                        completedResult = null;
                        return false;
                    }
                }

                return true;
            }

            if (result is IList list)
            {
                completedResult = operationContext.Result.RentResultList();
                completedResult.IsNullable = elementType.IsNullableType();

                for (var i = 0; i < list.Count; i++)
                {
                    if (TryComplete(
                            operationContext,
                            middlewareContext,
                            path.Append(i),
                            elementType,
                            list[i],
                            out object? completedElement) &&
                        completedElement is { } m)
                    {
                        completedResult.Add(m);
                        if (isElementList)
                        {
                            SetParent(m, completedResult);
                        }
                    }
                    else if (completedResult.IsNullable)
                    {
                        completedResult.Add(null);
                    }
                    else
                    {
                        completedResult = null;
                        return false;
                    }
                }

                return true;
            }

            if (result is IEnumerable enumerable)
            {
                var index = 0;
                completedResult = operationContext.Result.RentResultList();
                completedResult.IsNullable = elementType.IsNullableType();

                foreach (object? element in enumerable)
                {
                    if (TryComplete(
                            operationContext,
                            middlewareContext,
                            path.Append(index++),
                            elementType,
                            element,
                            out object? completedElement) &&
                        completedElement is { } m)
                    {
                        completedResult.Add(m);
                        if (isElementList)
                        {
                            SetParent(m, completedResult);
                        }
                    }
                    else if (completedResult.IsNullable)
                    {
                        completedResult.Add(null);
                    }
                    else
                    {
                        completedResult = null;
                        return false;
                    }
                }

                return true;
            }

            middlewareContext.ReportError(
                ListValueIsNotSupported(
                    result.GetType(),
                    middlewareContext.FieldSelection,
                    path));
            completedResult = null;
            return false;
        }

        private static void SetParent(object value, IResultData parentList)
        {
            if (value is IHasResultDataParent result)
            {
                result.Parent = parentList;
            }
        }

        private static bool TryCompleteLeafValue(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            ILeafType fieldType,
            object result,
            out object? completedResult)
        {
            try
            {
                if (!fieldType.RuntimeType.IsInstanceOfType(result) &&
                    operationContext.Converter.TryConvert(fieldType.RuntimeType, result, out var c))
                {
                    result = c;
                }
                completedResult = fieldType.Serialize(result);
                return true;
            }
            catch (SerializationException ex)
            {
                middlewareContext.ReportError(
                    InvalidLeafValue(
                        ex,
                        middlewareContext.FieldSelection,
                        path));
            }
            catch (Exception ex)
            {
                middlewareContext.ReportError(
                    UnexpectedLeafValueSerializationError(
                        ex,
                        operationContext.ErrorHandler,
                        middlewareContext.FieldSelection,
                        path));
            }

            completedResult = null;
            return true;
        }

        private static bool TryCompleteCompositeValue(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            IType fieldType,
            object result,
            [NotNullWhen(true)] out ResultMap? completedResult)
        {
            if (TryResolveObjectType(
                middlewareContext, path, fieldType, result,
                out ObjectType? objectType))
            {
                SelectionSetNode selectionSet =
                    middlewareContext.FieldSelection.SelectionSet!;

                ISelectionSet selections =
                    operationContext.CollectFields(selectionSet, objectType);

                completedResult = selections.EnqueueResolverTasks(
                    operationContext,
                    path.Append,
                    middlewareContext.ScopedContextData,
                    result);
                return true;
            }

            middlewareContext.ReportError(
                ValueCompletion_CouldNotResolveAbstractType(
                    middlewareContext.FieldSelection,
                    path));
            completedResult = null;
            return false;
        }

        private static bool TryResolveObjectType(
            IMiddlewareContext middlewareContext,
            Path path,
            IType fieldType,
            object result,
            [NotNullWhen(true)] out ObjectType? objectType)
        {
            try
            {
                if (middlewareContext.ValueType is ObjectType vot)
                {
                    objectType = vot;
                    return true;
                }

                if (fieldType is ObjectType ot)
                {
                    objectType = ot;
                    return true;
                }

                if (fieldType is InterfaceType it)
                {
                    objectType = it.ResolveConcreteType(middlewareContext, result);
                    return objectType is { };
                }

                if (fieldType is UnionType ut)
                {
                    objectType = ut.ResolveConcreteType(middlewareContext, result);
                    return objectType is { };
                }

                middlewareContext.ReportError(
                    UnableToResolveTheAbstractType(
                        fieldType.Print(),
                        middlewareContext.FieldSelection,
                        path));
            }
            catch (Exception ex)
            {
                middlewareContext.ReportError(
                    UnexpectedErrorWhileResolvingAbstractType(
                        ex,
                        fieldType.Print(),
                        middlewareContext.FieldSelection,
                        path));
            }

            objectType = null;
            return false;
        }
    }
}
