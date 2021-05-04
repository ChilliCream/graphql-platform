using System;
using System.Collections;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing
{
    internal static partial class ValueCompletion
    {
        private static bool TryCompleteListValue(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            ISelection selection,
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
                    selection,
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
                selection,
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
            ISelection selection,
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
                        selection,
                        path.Append(i),
                        elementType,
                        array.GetValue(i),
                        out var completedElement) &&
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
                            selection,
                            path.Append(i),
                            elementType,
                            list[i],
                            out var completedElement) &&
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

                foreach (var element in enumerable)
                {
                    if (TryComplete(
                            operationContext,
                            middlewareContext,
                            selection,
                            path.Append(index++),
                            elementType,
                            element,
                            out var completedElement) &&
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
                    selection.SyntaxNode,
                    path));
            completedResult = null;
            return false;
        }

        private static bool TryCompleteResultListValue(
           IOperationContext operationContext,
           IMiddlewareContext middlewareContext,
           ISelection selection,
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
                        selection,
                        path.Append(i),
                        elementType,
                        array.GetValue(i),
                        out var completedElement) &&
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
                            selection,
                            path.Append(i),
                            elementType,
                            list[i],
                            out var completedElement) &&
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

                foreach (var element in enumerable)
                {
                    if (TryComplete(
                            operationContext,
                            middlewareContext,
                            selection,
                            path.Append(index++),
                            elementType,
                            element,
                            out var completedElement) &&
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
                    selection.SyntaxNode,
                    path));
            completedResult = null;
            return false;
        }
    }
}
