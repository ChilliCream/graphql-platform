using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Client.Core;
using HotChocolate.Client.Internal;

namespace HotChocolate.Client
{
    public static class QueryableInterfaceExtensions
    {
        public static readonly MethodInfo CastMethod = GetMethodInfo(nameof(CastMethod));

        [MethodId(nameof(CastMethod))]
        public static TResult Cast<TResult>(this IQueryableInterface source)
            where TResult : IQueryableValue
        {
            var ctor = typeof(TResult).GetTypeInfo().DeclaredConstructors.FirstOrDefault(
                x =>
                {
                    var parameters = x.GetParameters();
                    return parameters.Length == 1 &&
                        parameters[0].ParameterType == typeof(Expression);
                });

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"Could not find {typeof(TResult).Name}(Expression) constructor");
            }

            var expression = Expression.Call(
                null,
                GetMethodInfoOf(() => Cast<TResult>(default(IQueryableInterface))),
                new Expression[] { source.Expression });

            return (TResult)ctor.Invoke(new[] { expression });
        }

        private static MethodInfo GetMethodInfo(string id)
        {
            return typeof(QueryableInterfaceExtensions)
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
