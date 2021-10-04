using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Utilities.StreamAdapters;

namespace HotChocolate.Execution.Processing
{
    public sealed partial class OperationCompiler
    {
        private delegate IAsyncEnumerable<object?> CreateStreamDelegate(object obj);

        private static readonly MethodInfo _createGenericStream =
            typeof(OperationCompiler)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .First(m => m.Name.EqualsOrdinal(nameof(CreateStreamFromResult)) &&
                            m.IsGenericMethod);

        private static readonly ParameterExpression _resolverResultParam =
            Expression.Parameter(typeof(object), "o");

        private static readonly ConcurrentDictionary<Type, CreateStreamDelegate> _cache = new();

        private static CreateStreamDelegate CreateStream(Type runtimeType)
        {
            if (runtimeType != typeof(object))
            {
                return _cache.GetOrAdd(runtimeType, r =>
                {
                    Expression callMethod = Expression.Call(
                        _createGenericStream.MakeGenericMethod(r),
                        _resolverResultParam);

                    Expression<CreateStreamDelegate> lambda =
                        Expression.Lambda<CreateStreamDelegate>(callMethod, _resolverResultParam);

                    return lambda.Compile();
                });
            }

            return CreateStreamFromResult;
        }

        private static IAsyncEnumerable<object?> CreateStreamFromResult<T>(object result)
        {
            return result switch
            {
                IAsyncEnumerable<object?> stream => stream,
                IAsyncEnumerable<T> stream => new AsyncEnumerableStreamAdapter<T>(stream),
                IQueryable<T> query => new QueryableStreamAdapter<T>(query),
                IQueryable query => new QueryableStreamAdapter(query),
                IEnumerable<T> enumerable => new EnumerableStreamAdapter<T>(enumerable),
                IEnumerable enumerable => new EnumerableStreamAdapter(enumerable),
                _ => throw new NotSupportedException()
            };
        }

        private static IAsyncEnumerable<object?> CreateStreamFromResult(object result)
        {
            return result switch
            {
                IAsyncEnumerable<object?> stream => stream,
                IQueryable query => new QueryableStreamAdapter(query),
                IEnumerable enumerable => new EnumerableStreamAdapter(enumerable),
                _ => throw new NotSupportedException()
            };
        }
    }
}
