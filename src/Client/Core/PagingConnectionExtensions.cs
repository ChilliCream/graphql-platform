using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Client.Core;
using HotChocolate.Client.Internal;

namespace HotChocolate.Client
{
    public static class PagingConnectionExtensions
    {
        public static readonly MethodInfo AllPagesMethod = GetMethodInfo(nameof(AllPages), 1);
        public static readonly MethodInfo AllPagesCustomSizeMethod = GetMethodInfo(nameof(AllPages), 2);

        [MethodId(nameof(AllPages))]
        public static IQueryableList<TResult> AllPages<TResult>(this IPagingConnection<TResult> source)
            where TResult : IQueryableValue
        {
            return new QueryableList<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfoOf(() => AllPages<TResult>(default)),
                    new Expression[] { source.Expression }));
        }

        [MethodId(nameof(AllPages))]
        public static IQueryableList<TResult> AllPages<TResult>(this IPagingConnection<TResult> source, int pageSize)
            where TResult : IQueryableValue
        {
            return new QueryableList<TResult>(Expression.Call(
                null,
                GetMethodInfoOf(() => AllPages<TResult>(default, pageSize)),
                new Expression[]{ source.Expression, Expression.Constant(pageSize) }));
        }

        private static MethodInfo GetMethodInfo(string id, int parameterCount)
        {
            return typeof(PagingConnectionExtensions)
                .GetTypeInfo()
                .DeclaredMethods
                .Where(x => x.GetCustomAttribute<MethodIdAttribute>()?.Id == id && x.GetParameters().Length == parameterCount)
                .SingleOrDefault();
        }

        private static MethodInfo GetMethodInfoOf<T>(Expression<Func<T>> expression)
        {
            var body = (MethodCallExpression)expression.Body;
            return body.Method;
        }
    }
}
