using System;
using System.Collections;
using System.Text.Json;
using HotChocolate.Types;
using static HotChocolate.Execution.ErrorHelper;
using static HotChocolate.Execution.Processing.PathHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    private static object? CompleteListValue(
        ValueCompletionContext context,
        ISelection selection,
        IType type,
        ResultData parent,
        int index,
        object? result)
    {
        if (result is null)
        {
            return null;
        }

        var elementType = type.InnerType();
        var isLeafType = elementType.IsLeafType();
        var operationContext = context.OperationContext;
        var resolverContext = context.ResolverContext;

        if (result is Array array)
        {
            var resultList = operationContext.Result.RentList(array.Length);
            resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;
            resultList.SetParent(parent, index);

            for (var i = 0; i < array.Length; i++)
            {
                var elementResult = array.GetValue(i);

                if (!TryCompleteElement(context, selection, elementType, isLeafType, resultList, i, elementResult))
                {
                    operationContext.Result.AddRemovedResult(resultList);
                    return null;
                }
            }

            return resultList;
        }

        if (result is IList list)
        {
            var resultList = operationContext.Result.RentList(list.Count);
            resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;
            resultList.SetParent(parent, index);

            for (var i = 0; i < list.Count; i++)
            {
                if (!TryCompleteElement(context, selection, elementType, isLeafType, resultList, i, list[i]))
                {
                    operationContext.Result.AddRemovedResult(resultList);
                    return null;
                }
            }

            return resultList;
        }

        if (result is IEnumerable enumerable)
        {
            var resultList = operationContext.Result.RentList(4);
            resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;
            resultList.SetParent(parent, index);

            var i = 0;

            foreach (var element in enumerable)
            {
                if (resultList.Count == resultList.Capacity)
                {
                    resultList.Grow();
                }

                if (!TryCompleteElement(context, selection, elementType, isLeafType, resultList, i++, element))
                {
                    operationContext.Result.AddRemovedResult(resultList);
                    return null;
                }
            }

            return resultList;
        }

        if (result is JsonElement { ValueKind: JsonValueKind.Array, } node)
        {
            var resultList = operationContext.Result.RentList(4);
            resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;
            resultList.SetParent(parent, index);

            var i = 0;
            foreach (var element in node.EnumerateArray())
            {
                if (resultList.Count == resultList.Capacity)
                {
                    resultList.Grow();
                }

                if (!TryCompleteElement(context, selection, elementType, isLeafType, resultList, i++, element))
                {
                    operationContext.Result.AddRemovedResult(resultList);
                    return null;
                }
            }

            return resultList;
        }

        var errorPath = CreatePathFromContext(selection, parent, index);
        var error = ListValueIsNotSupported(result.GetType(), selection.SyntaxNode, errorPath);
        operationContext.ReportError(error, resolverContext, selection);

        return null;
    }

    private static bool TryCompleteElement(
        ValueCompletionContext context,
        ISelection selection,
        IType elementType,
        bool isLeafType,
        ListResult list,
        int parentIndex,
        object? elementResult)
    {
        // We first add a null entry so that the null-propagation has an element to traverse.
        var index = list.AddUnsafe(null);
        var completedElement = Complete(context, selection, elementType, list, parentIndex, elementResult);

        if (completedElement is not null)
        {
            if (isLeafType)
            {
                list.SetUnsafe(index, completedElement);
            }
            else
            {
                var resultData = (ResultData)completedElement;

                if (resultData.IsInvalidated)
                {
                    return list.IsNullable;
                }

                list.SetUnsafe(index, resultData);
            }
            return true;
        }

        return list.IsNullable;
    }

    internal static void PropagateNullValues(ResultData result)
    {
        if(result.IsInvalidated)
        {
            return;
        }

        result.IsInvalidated = true;

        while (result.Parent is not null)
        {
            var index = result.ParentIndex;
            var parent = result.Parent;

            if(parent.IsInvalidated)
            {
                return;
            }

            switch (parent)
            {
                case ObjectResult objectResult:
                    var field = objectResult[index];
                    if(field.TrySetNull())
                    {
                        return;
                    }
                    objectResult.IsInvalidated = true;
                    break;

                case ListResult listResult:
                    if (listResult.TrySetNull(index))
                    {
                        return;
                    }
                    listResult.IsInvalidated = true;
                    break;
            }

            result = parent;
        }
    }
}
