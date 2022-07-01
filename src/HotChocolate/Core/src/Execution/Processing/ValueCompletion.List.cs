using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Types;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    private static readonly IReadOnlyList<ParameterExpression> _completeListParams = new[]
    {
        Expression.Parameter(typeof(IOperationContext)),
        Expression.Parameter(typeof(MiddlewareContext)),
        Expression.Parameter(typeof(ISelection)),
        Expression.Parameter(typeof(Path)),
        Expression.Parameter(typeof(IType)),
        Expression.Parameter(typeof(string)),
        Expression.Parameter(typeof(int)),
        Expression.Parameter(typeof(object)),
        Expression.Parameter(typeof(List<ResolverTask>))
    };

    private static readonly MethodInfo _completeArrayMethod =
        typeof(ValueCompletion).GetMethod(
            nameof(CompleteArrayInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

#if NET5_0_OR_GREATER
    private static readonly MethodInfo _completeListMethod =
        typeof(ValueCompletion).GetMethod(
            nameof(CompleteListInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;
#endif

    private static readonly MethodInfo _completeEnumerableMethod =
        typeof(ValueCompletion).GetMethod(
            nameof(CompleteEnumerableInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly ConcurrentDictionary<Type, CompleteList> _compiledListDelegates = new();

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

        if (_compiledListDelegates.TryGetValue(resultType, out var complete))
        {
            completedValue = complete(
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

        if (resultType.IsArray)
        {
            var method = _completeArrayMethod.MakeGenericMethod(resultType.GetElementType()!);
            var call = Expression.Call(method, _completeListParams);
            complete = Expression.Lambda<CompleteList>(call, _completeListParams).Compile();
            _compiledListDelegates.TryAdd(resultType, complete);
        }
#if NET5_0_OR_GREATER
        else if (resultType.IsGenericType &&
            resultType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var method = _completeListMethod.MakeGenericMethod(resultType);
            var call = Expression.Call(method, _completeListParams);
            complete = Expression.Lambda<CompleteList>(call, _completeListParams).Compile();
            _compiledListDelegates.TryAdd(resultType, complete);
        }
#endif
        else if (typeof(IEnumerable).IsAssignableFrom(resultType))
        {
            var interfaceTypes = resultType.GetInterfaces();
            Type? enumerableType = null;

            for (var i = 0; i < interfaceTypes.Length; i++)
            {
                var interfaceType = interfaceTypes[i];

                if (interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    enumerableType = interfaceType;
                    break;
                }
            }

            if (enumerableType is not null)
            {
                var elementType = enumerableType.GetGenericArguments()[0];
                var method = _completeEnumerableMethod.MakeGenericMethod(resultType, elementType);
                var call = Expression.Call(method, _completeListParams);
                complete = Expression.Lambda<CompleteList>(call, _completeListParams).Compile();
                _compiledListDelegates.TryAdd(resultType, complete);
            }
        }

        if (complete is not null)
        {
            completedValue = complete(
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

    private static object? CompleteArrayInternal<T>(
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
        var array = (T[])result;
        var arrayLength = array.Length;
        var elementType = fieldType.InnerType();
        var isLeaf = elementType.IsLeafType();

        var resultList = operationContext.Result.RentList(arrayLength);
        resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;
        ref var start = ref MemoryMarshal.GetReference(array.AsSpan());

        for (var i = 0; i < arrayLength; i++)
        {
            var elementResult = Unsafe.Add(ref start, i);
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

                if (isLeaf)
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

#if NET5_0_OR_GREATER
     private static object? CompleteListInternal<T>(
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
        var list = (List<T>)result;
        var listLength = list.Count;
        var elementType = fieldType.InnerType();
        var isLeaf = elementType.IsLeafType();

        var resultList = operationContext.Result.RentList(listLength);
        resultList.IsNullable = elementType.Kind is not TypeKind.NonNull;

        var span = CollectionsMarshal.AsSpan(list);
        ref var start = ref MemoryMarshal.GetReference(span);

        for (var i = 0; i < listLength; i++)
        {
            var elementResult = Unsafe.Add(ref start, i);
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

                if (isLeaf)
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
#endif

     private static object? CompleteEnumerableInternal<TEnumerable, TValue>(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        IType fieldType,
        string responseName,
        int responseIndex,
        object result,
        List<ResolverTask> bufferedTasks)
        where TEnumerable : IEnumerable<TValue>
    {
        var index = 0;
        var enumerable = (TEnumerable)result;
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

                if (isLeaf)
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

    private delegate object? CompleteList(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        IType fieldType,
        string responseName,
        int responseIndex,
        object result,
        List<ResolverTask> bufferedTasks);
}
