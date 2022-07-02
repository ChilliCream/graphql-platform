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
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        List<ResolverTask> tasks,
        ISelection selection,
        Path path,
        IType fieldType,
        string responseName,
        int responseIndex,
        object? result)
    {
        var resultList = operationContext.Result.RentResultList();
        var elementType = fieldType.InnerType();
        resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;
        var isLeafType = elementType.IsLeafType();

        if (result is Array array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                var elementPath = operationContext.PathFactory.Append(path, i);
                if (!TryCompleteElement(elementPath, array.GetValue(i)))
                {
                    return null;
                }
            }

            return resultList;
        }

        if (result is IList list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var elementPath = operationContext.PathFactory.Append(path, i);
                if (!TryCompleteElement(elementPath, list[i]))
                {
                    return null;
                }
            }

            return resultList;
        }

        if (result is IEnumerable enumerable)
        {
            var index = 0;
            foreach (var element in enumerable)
            {
                var elementPath = operationContext.PathFactory.Append(path, index++);
                if (!TryCompleteElement(elementPath, element))
                {
                    return null;
                }
            }

            return resultList;
        }

        ReportError(
            operationContext,
            resolverContext,
            selection,
            ListValueIsNotSupported(resultList.GetType(), selection.SyntaxNode, path));

        return null;

        bool TryCompleteElement(Path elementPath, object? elementResult)
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
                resultList.Add(completedElement);

                if (!isLeafType)
                {
                    ((IHasResultDataParent)completedElement).Parent = resultList;
                }

                return true;
            }

            if (resultList.IsNullable)
            {
                resultList.Add(null);
                return true;
            }

            return false;
        }
    }
}
