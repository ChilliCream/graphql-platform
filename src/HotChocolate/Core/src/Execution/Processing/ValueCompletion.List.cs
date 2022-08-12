using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Types;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    private static object? CompleteListValue(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        List<ResolverTask> tasks,
        ISelection selection,
        Path path,
        IType fieldType,
        string responseName,
        int responseIndex,
        object? result)
    {
        var elementType = fieldType.InnerType();
        var isLeafType = elementType.IsLeafType();

        if (result is Array array)
        {
            var resultList = operationContext.Result.RentList(array.Length);
            resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;

            for (var i = 0; i < array.Length; i++)
            {
                var elementResult = array.GetValue(i);
                var elementPath = operationContext.PathFactory.Append(path, i);

                if (!TryCompleteElement(resultList, elementPath, elementResult))
                {
                    return null;
                }
            }

            return resultList;
        }

        if (result is IList list)
        {
            var resultList = operationContext.Result.RentList(list.Count);
            resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;

            for (var i = 0; i < list.Count; i++)
            {
                var elementPath = operationContext.PathFactory.Append(path, i);
                if (!TryCompleteElement(resultList, elementPath, list[i]))
                {
                    return null;
                }
            }

            return resultList;
        }

        if (result is IEnumerable enumerable)
        {
            var resultList = operationContext.Result.RentList(4);
            resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;

            var index = 0;
            foreach (var element in enumerable)
            {
                if (resultList.Count == resultList.Capacity)
                {
                    resultList.Grow();
                }

                var elementPath = operationContext.PathFactory.Append(path, index++);
                if (!TryCompleteElement(resultList, elementPath, element))
                {
                    return null;
                }
            }

            return resultList;
        }

        var error = ListValueIsNotSupported(typeof(ListResult), selection.SyntaxNode, path);
        operationContext.ReportError(error, resolverContext, selection);

        return null;

        bool TryCompleteElement(ListResult resultList, Path elementPath, object? elementResult)
        {
            var completedElement = Complete(
                operationContext,
                resolverContext,
                tasks,
                selection,
                elementPath,
                elementType,
                responseName,
                responseIndex,
                elementResult);

            if (completedElement is not null)
            {
                resultList.AddUnsafe(completedElement);

                if (!isLeafType)
                {
                    ((ResultData)completedElement).Parent = resultList;
                }

                return true;
            }

            if (resultList.IsNullable)
            {
                resultList.AddUnsafe(null);
                return true;
            }

            return false;
        }
    }
}
