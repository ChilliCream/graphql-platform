using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Client.Core;
using HotChocolate.Client.Core.Builders;
using HotChocolate.Client.Internal;

namespace HotChocolate.Client
{
    public static class QueryableValueExtensions
    {
        public static readonly MethodInfo SelectMethod = GetMethodInfo(nameof(SelectMethod));
        public static readonly MethodInfo SelectFragmentMethod = GetMethodInfo(nameof(SelectFragmentMethod));
        public static readonly MethodInfo SelectListMethod = GetMethodInfo(nameof(SelectListMethod));
        public static readonly MethodInfo SingleMethod = GetMethodInfo(nameof(SingleMethod));
        public static readonly MethodInfo SingleOrDefaultMethod = GetMethodInfo(nameof(SingleOrDefaultMethod));

        [MethodId(nameof(SelectMethod))]
        public static IQueryableValue<TResult> Select<TValue, TResult>(
            this IQueryableValue<TValue> source,
            Expression<Func<TValue, TResult>> selector)
                where TValue : IQueryableValue
        {
            return new QueryableValue<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Select(
                        default(IQueryableValue<TValue>),
                        default(Expression<Func<TValue, TResult>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }));
        }

        [MethodId(nameof(SelectFragmentMethod))]
        public static IQueryableValue<TResult> Select<TValue, TResult>(
            this IQueryableValue<TValue> source,
            Fragment<TValue, TResult> fragment)
                where TValue : IQueryableValue
        {
            return new QueryableValue<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Select(
                        default(IQueryableValue<TValue>),
                        default(Fragment<TValue, TResult>))),
                    new Expression[] { source.Expression, Expression.Constant(fragment) }));
        }

        [MethodId(nameof(SelectListMethod))]
        public static IQueryableList<TResult> Select<TValue, TResult>(
            this IQueryableValue<TValue> source,
            Expression<Func<TValue, IQueryableList<TResult>>> selector)
                where TValue : IQueryableValue
        {
            return new QueryableList<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => Select(
                        default(IQueryableValue<TValue>),
                        default(Expression<Func<TValue, IQueryableList<TResult>>>))),
                    new Expression[] { source.Expression, Expression.Quote(selector) }));
        }

        [MethodId(nameof(SingleMethod))]
        public static TValue Single<TValue>(this IQueryableValue<TValue> source)
        {
            throw new NotImplementedException();
        }

        [MethodId(nameof(SingleOrDefaultMethod))]
        public static TValue SingleOrDefault<TValue>(this IQueryableValue<TValue> source)
        {
            throw new NotImplementedException();
        }

        public static ICompiledQuery<T> Compile<T>(this IQueryableValue<T> expression)
        {
            return new QueryBuilder().Build(expression);
        }

        private static MethodInfo GetMethodInfo(string id)
        {
            return typeof(QueryableValueExtensions)
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
