using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution.Processing.Pooling;
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
        IType elementType = fieldType.InnerType();

        if (result is Array array)
        {
            ObjectListResult listResult = operationContext.Result.RentObjectList(array.Length);
            listResult.IsNullable = elementType.Kind is not TypeKind.NonNull;

            for (var i = 0; i < array.Length; i++)
            {
                if (!TryCompleteElement(listResult, path.Append(i), array.GetValue(i)))
                {
                    completedResult = null;
                    return true;
                }
            }

            completedResult = listResult;
            return true;
        }

        if (result is IList list)
        {
            ObjectListResult listResult = operationContext.Result.RentObjectList(list.Count);
            listResult.IsNullable = elementType.Kind is not TypeKind.NonNull;

            for (var i = 0; i < list.Count; i++)
            {
                if (!TryCompleteElement(listResult, path.Append(i), list[i]))
                {
                    completedResult = null;
                    return true;
                }
            }

            completedResult = listResult;
            return true;
        }

        if (result is IEnumerable enumerable)
        {
            ObjectListResult listResult = operationContext.Result.RentObjectList(4);
            listResult.IsNullable = elementType.Kind is not TypeKind.NonNull;
            var index = 0;

            foreach (var element in enumerable)
            {
                if (listResult.Capacity == listResult.Count)
                {
                    listResult.Grow();
                }

                if (!TryCompleteElement(listResult, path.Append(index++), element))
                {
                    completedResult = null;
                    return true;
                }
            }

            completedResult = listResult;
            return true;
        }

        ReportError(
            operationContext,
            resolverContext,
            selection,
            ListValueIsNotSupported(typeof(ObjectListResult), selection.SyntaxNode, path));

        completedResult = null;
        return false;

        bool TryCompleteElement(ObjectListResult listResult, Path elementPath, object? elementResult)
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
                completedElement is ObjectResult objectResult)
            {
                objectResult.Parent = listResult;
                listResult.AddUnsafe(objectResult);
            }
            else if (listResult.IsNullable)
            {
                listResult.AddUnsafe(null);
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
        IType elementType = fieldType.InnerType();
        var isElementList = elementType.IsListType();

        if (result is Array array)
        {
            ListResult listResult = operationContext.Result.RentList(array.Length);
            listResult.IsNullable = elementType.Kind is not TypeKind.NonNull;

            for (var i = 0; i < array.Length; i++)
            {
                if (!TryCompleteElement(listResult, path.Append(i), array.GetValue(i)))
                {
                    completedResult = null;
                    return true;
                }
            }

            completedResult = listResult;
            return true;
        }

        if (result is IList list)
        {
            ListResult listResult = operationContext.Result.RentList(list.Count);
            listResult.IsNullable = elementType.Kind is not TypeKind.NonNull;

            for (var i = 0; i < list.Count; i++)
            {
                if (!TryCompleteElement(listResult, path.Append(i), list[i]))
                {
                    completedResult = null;
                    return true;
                }
            }

            completedResult = listResult;
            return true;
        }

        if (result is IEnumerable enumerable)
        {
            ListResult listResult = operationContext.Result.RentList(4);
            listResult.IsNullable = elementType.Kind is not TypeKind.NonNull;
            var index = 0;

            foreach (var element in enumerable)
            {
                if (listResult.Capacity == listResult.Count)
                {
                    listResult.Grow();
                }

                if (!TryCompleteElement(listResult, path.Append(index++), element))
                {
                    completedResult = null;
                    return true;
                }
            }

            completedResult = listResult;
            return true;
        }

        ReportError(
            operationContext,
            resolverContext,
            selection,
            ListValueIsNotSupported(typeof(ListResult), selection.SyntaxNode, path));

        completedResult = null;
        return false;

        bool TryCompleteElement(ListResult listResult, Path elementPath, object? elementResult)
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
                listResult.AddUnsafe(completedElement);

                if (isElementList)
                {
                    ((ResultData)completedElement).Parent = listResult;
                }
            }
            else if (listResult.IsNullable)
            {
                listResult.AddUnsafe(null);
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
