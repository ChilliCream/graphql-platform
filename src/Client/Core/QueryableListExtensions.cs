using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Client.Core;
using HotChocolate.Client.Core.Builders;
using HotChocolate.Client.Internal;

namespace HotChocolate.Client
{
    public static class QueryableListExtensions
    {
        public static readonly MethodInfo OfTypeMethod = GetMethodInfo(nameof(OfTypeMethod));
        public static readonly MethodInfo SelectMethod = GetMethodInfo(nameof(SelectMethod));
        public static readonly MethodInfo SelectFragmentMethod = GetMethodInfo(nameof(SelectFragmentMethod));
        public static readonly MethodInfo ToDictionaryMethod = GetMethodInfo(nameof(ToDictionaryMethod));
        public static readonly MethodInfo ToListMethod = GetMethodInfo(nameof(ToListMethod));

        [MethodId(nameof(OfTypeMethod))]
        public static IQueryableList<TResult> OfType<TResult>(this IQueryableList source)
            where TResult : IQueryableValue
        {
            return new QueryableList<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => OfType<TResult>(default(IQueryableList))),
                    new Expression[] { source.Expression }));
        }

        [MethodId(nameof(SelectMethod))]
        public static IQueryableList<TResult> Select<TValue, TResult>(
            this IQueryableList<TValue> source,
            Expression<Func<TValue, TResult>> selector)
                where TValue : IQueryableValue
        {
            return new QueryableList<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Select(
                        default(IQueryableList<TValue>),
                        default(Expression<Func<TValue, TResult>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }));
        }

        [MethodId(nameof(SelectFragmentMethod))]
        public static IQueryableList<TResult> Select<TValue, TResult>(
            this IQueryableList<TValue> source,
            IFragment<TValue, TResult> fragment)
                where TValue : IQueryableValue
        {
            return new QueryableList<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Select(
                        default(IQueryableList<TValue>),
                        default(Expression<Func<TValue, TResult>>))),
                    new Expression[] { source.Expression, Expression.Quote(fragment.Expression) }));
        }

        [MethodId(nameof(ToDictionaryMethod))]
        public static IDictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
            this IQueryableList<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
        {
            throw new NotImplementedException();
        }

        [MethodId(nameof(ToListMethod))]
        public static List<TValue> ToList<TValue>(this IQueryableList<TValue> source)
        {
            throw new NotImplementedException();
        }

        public static ICompiledQuery<IEnumerable<T>> Compile<T>(this IQueryableList<T> expression)
        {
            return new QueryBuilder().Build(expression);
        }

        private static MethodInfo GetMethodInfo(string id)
        {
            return typeof(QueryableListExtensions)
                .GetTypeInfo()
                .DeclaredMethods
                .Where(x => x.GetCustomAttribute<MethodIdAttribute>()?.Id == id)
                .SingleOrDefault();
        }

        private static MethodInfo GetMethodInfoOf<T>(Expression<Func<T>> expression)
        {
            var body = (MethodCallExpression)expression.Body;
            return body.Method;
        }
    }
}
