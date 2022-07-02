using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        object result,
        List<ResolverTask> bufferedTasks,
        out object? completedValue)
    {
        var resultType = result.GetType();

        if (resultType.IsArray)
        {
            completedValue =
                CompleteArrayInternal(
                    operationContext,
                    resolverContext,
                    selection,
                    path,
                    fieldType,
                    responseName,
                    responseIndex,
                    result,
                    bufferedTasks);
            return true;
        }

        if (typeof(IList).IsAssignableFrom(resultType))
        {
            completedValue =
                CompleteListInternal(
                    operationContext,
                    resolverContext,
                    selection,
                    path,
                    fieldType,
                    responseName,
                    responseIndex,
                    result,
                    bufferedTasks);
            return true;
        }

        if (typeof(IEnumerable).IsAssignableFrom(resultType))
        {
            completedValue =
                CompleteEnumerableInternal(
                    operationContext,
                    resolverContext,
                    selection,
                    path,
                    fieldType,
                    responseName,
                    responseIndex,
                    result,
                    bufferedTasks);
            return true;
        }

        ReportError(
            operationContext,
            resolverContext,
            selection,
            ListValueIsNotSupported(typeof(ListResult), selection.SyntaxNode, path));

        completedValue = null;
        return false;
    }

    private static object? CompleteArrayInternal(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        IType fieldType,
        string responseName,
        int responseIndex,
        object result,
        List<ResolverTask> bufferedTasks)
    {
        var array = (Array)result;
        var arrayLength = array.Length;
        var elementType = fieldType.InnerType();
        var isLeaf = elementType.IsLeafType();

        var resultList = operationContext.Result.RentList(arrayLength);
        resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;

#if NET6_0_OR_GREATER
        ref var start = ref MemoryMarshal.GetArrayDataReference(array);
#endif

        for (var i = 0; i < arrayLength; i++)
        {
#if NET6_0_OR_GREATER
            ref var elementRef = ref Unsafe.Add(ref start, i);
            ref var elementResult = ref Unsafe.As<byte, object>(ref elementRef);
#else
            var elementResult = array.GetValue(i);
#endif
            var elementPath = operationContext.PathFactory.Append(path, i);

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
                resultList.AddUnsafe(completedElement);

                if (!isLeaf)
                {
                    ((ResultData)completedElement).Parent = resultList;
                }
            }
            else if (resultList.IsNullable)
            {
                resultList.AddUnsafe(null);
            }
            else
            {
                // if the element cannot be completed due to non-null constraints we will
                // return null for the list.
                // the rented result list is tracked by the execution and will be returned
                // after the request is completed.
                return null;
            }
        }

        return resultList;
    }

    private static object? CompleteListInternal(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        IType fieldType,
        string responseName,
        int responseIndex,
        object result,
        List<ResolverTask> bufferedTasks)
    {
        var list = (IList)result;
        var listLength = list.Count;
        var elementType = fieldType.InnerType();
        var isLeaf = elementType.IsLeafType();

        var resultList = operationContext.Result.RentList(listLength);
        resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;

        for (var i = 0; i < listLength; i++)
        {
            var elementResult = list[i];
            var elementPath = operationContext.PathFactory.Append(path, i);

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
                resultList.AddUnsafe(completedElement);

                if (!isLeaf)
                {
                    ((ResultData)completedElement).Parent = resultList;
                }
            }
            else if (resultList.IsNullable)
            {
                resultList.AddUnsafe(null);
            }
            else
            {
                // if the element cannot be completed due to non-null constraints we will
                // return null for the list.
                // the rented result list is tracked by the execution and will be returned
                // after the request is completed.
                return null;
            }
        }

        return resultList;
    }

     private static object? CompleteEnumerableInternal(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        IType fieldType,
        string responseName,
        int responseIndex,
        object result,
        List<ResolverTask> bufferedTasks)
     {
        var index = 0;
        var enumerable = (IEnumerable)result;
        var elementType = fieldType.InnerType();
        var isLeaf = elementType.IsLeafType();
        var resultList = operationContext.Result.RentList(4);
        resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;

        foreach (var elementResult in enumerable)
        {
            if (resultList.Capacity == resultList.Count)
            {
                resultList.Grow();
            }

            var elementPath = operationContext.PathFactory.Append(path, index++);

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
                resultList.AddUnsafe(completedElement);

                if (!isLeaf)
                {
                    ((ResultData)completedElement).Parent = resultList;
                }
            }
            else if (resultList.IsNullable)
            {
                resultList.AddUnsafe(null);
            }
            else
            {
                // if the element cannot be completed due to non-null constraints we will
                // return null for the list.
                // the rented result list is tracked by the execution and will be returned
                // after the request is completed.
                return null;
            }
        }

        return resultList;
    }
}
