using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Types;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    private static bool TryCompleteListValue(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        IType fieldType,
        string responseName,
        int responseIndex,
        object? result,
        List<ResolverTask> bufferedTasks,
        out object? completedValue)
    {
        IType elementType = fieldType.InnerType();

        if (elementType.Kind is TypeKind.NonNull)
        {
            elementType = elementType.InnerType();
        }

        if (elementType.Kind is TypeKind.Object or TypeKind.Interface or TypeKind.Union)
        {
            return TryCompleteCompositeListValue(
                operationContext,
                resolverContext,
                selection,
                path,
                fieldType,
                responseName,
                responseIndex,
                result,
                bufferedTasks,
                out completedValue);
        }

        return TryCompleteOtherListValue(
            operationContext,
            resolverContext,
            selection,
            path,
            fieldType,
            responseName,
            responseIndex,
            result,
            bufferedTasks,
            out completedValue);
    }

    private static bool TryCompleteCompositeListValue(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        IType fieldType,
        string responseName,
        int responseIndex,
        object? result,
        List<ResolverTask> bufferedTasks,
        out object? completedResult)
    {
        ResultMapList resultList = operationContext.Result.RentResultMapList();
        IType elementType = fieldType.InnerType();
        resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;

        if (result is Array array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                if (!TryCompleteElement(
                        operationContext.PathFactory.Append(path,i),
                        array.GetValue(i)))
                {
                    completedResult = null;
                    return true;
                }
            }

            completedResult = resultList;
            return true;
        }

        if (result is IList list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (!TryCompleteElement(operationContext.PathFactory.Append(path, i), list[i]))
                {
                    completedResult = null;
                    return true;
                }
            }

            completedResult = resultList;
            return true;
        }

        if (result is IEnumerable enumerable)
        {
            var index = 0;

            foreach (var element in enumerable)
            {
                if (!TryCompleteElement(
                        operationContext.PathFactory.Append(path, index++), element))
                {
                    completedResult = null;
                    return true;
                }
            }

            completedResult = resultList;
            return true;
        }

        ReportError(
            operationContext,
            resolverContext,
            selection,
            ListValueIsNotSupported(resultList.GetType(), selection.SyntaxNode, path));

        completedResult = null;
        return false;

        bool TryCompleteElement(Path elementPath, object? elementResult)
        {
            if (TryComplete(
                operationContext,
                resolverContext,
                selection,
                elementPath,
                elementType,
                responseName,
                responseIndex,
                elementResult,
                bufferedTasks,
                out var completedElement) &&
                completedElement is ResultMap resultMap)
            {
                resultMap.Parent = resultList;
                resultList.Add(resultMap);
            }
            else if (resultList.IsNullable)
            {
                resultList.Add(null);
            }
            else
            {
                return false;
            }

            return true;
        }
    }

    private static bool TryCompleteOtherListValue(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        IType fieldType,
        string responseName,
        int responseIndex,
        object? result,
        List<ResolverTask> bufferedTasks,
        out object? completedResult)
    {
        ResultList resultList = operationContext.Result.RentResultList();
        IType elementType = fieldType.InnerType();
        resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;
        var isElementList = elementType.IsListType();

        if (result is Array array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                if (!TryCompleteElement(
                        operationContext.PathFactory.Append(path, i),
                        array.GetValue(i)))
                {
                    completedResult = null;
                    return true;
                }
            }

            completedResult = resultList;
            return true;
        }

        if (result is IList list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (!TryCompleteElement(operationContext.PathFactory.Append(path, i), list[i]))
                {
                    completedResult = null;
                    return true;
                }
            }

            completedResult = resultList;
            return true;
        }

        if (result is IEnumerable enumerable)
        {
            var index = 0;

            foreach (var element in enumerable)
            {
                if (!TryCompleteElement(
                        operationContext.PathFactory.Append(path, index++),
                        element))
                {
                    completedResult = null;
                    return true;
                }
            }

            completedResult = resultList;
            return true;
        }

        ReportError(
            operationContext,
            resolverContext,
            selection,
            ListValueIsNotSupported(resultList.GetType(), selection.SyntaxNode, path));

        completedResult = null;
        return false;

        bool TryCompleteElement(Path elementPath, object? elementResult)
        {
            if (TryComplete(
                operationContext,
                resolverContext,
                selection,
                elementPath,
                elementType,
                responseName,
                responseIndex,
                elementResult,
                bufferedTasks,
                out var completedElement) &&
                completedElement is not null)
            {
                resultList.Add(completedElement);

                if (isElementList)
                {
                    ((IHasResultDataParent)completedElement).Parent = resultList;
                }
            }
            else if (resultList.IsNullable)
            {
                resultList.Add(null);
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
