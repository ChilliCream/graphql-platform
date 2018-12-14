using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal delegate T MiddlewareFactory<T>(
        IServiceProvider services,
        QueryDelegate next);

    internal delegate Task ClassQueryDelegate<T>(
        IQueryContext context,
        IServiceProvider services,
        T middleware);

    internal static class MiddlewareActivator
    {
        internal static MiddlewareFactory<T> CompileFactory<T>()
        {
            var services = Expression.Parameter(typeof(IServiceProvider));
            var nextDelegate = Expression.Parameter(typeof(QueryDelegate));

            NewExpression createInstance = CreateMiddleware(
                typeof(T), services, nextDelegate);

            return Expression.Lambda<MiddlewareFactory<T>>(
                createInstance, services, nextDelegate)
                .Compile();
        }

        internal static ClassQueryDelegate<T> CompileMiddleware<T>()
        {
            Type middlewareType = typeof(T);
            MethodInfo method = middlewareType.GetMethod("InvokeAsync")
                ?? middlewareType.GetMethod("Invoke");

            if (method == null)
            {
                // TODO : Resources
                throw new NotSupportedException(
                    "The provided middleware type must contain " +
                    "an invoke method.");
            }

            var context = Expression.Parameter(typeof(IQueryContext));
            var services = Expression.Parameter(typeof(IServiceProvider));
            var middlewareInstance = Expression.Parameter(middlewareType);

            var middlewareCall = Expression.Call(
                middlewareInstance,
                method,
                CreateParameters(method, services, context));

            return Expression.Lambda<ClassQueryDelegate<T>>(
                middlewareCall, context, services, middlewareInstance)
                .Compile();
        }

        private static NewExpression CreateMiddleware(
            Type middleware,
            ParameterExpression services,
            Expression next)
        {
            ConstructorInfo constructor = CreateConstructor(middleware);
            IEnumerable<Expression> arguments =
                CreateParameters(constructor, services, next);
            return Expression.New(constructor, arguments);
        }

        private static ConstructorInfo CreateConstructor(Type middleware)
        {
            var constructors = middleware.GetConstructors();
            if (constructors.Length == 1)
            {
                return constructors[0];
            }

            // TODO : Resources
            throw new NotSupportedException(
                "Middleware classes must have exactly " +
                "one public constructor.");
        }

        private static IEnumerable<Expression> CreateParameters(
            MethodInfo invokeMethod,
            ParameterExpression services,
            Expression context)
        {
            return CreateParameters(
                invokeMethod.GetParameters(),
                services,
                new Dictionary<Type, Expression>
                {
                    { typeof(IQueryContext), context }
                });
        }

        private static IEnumerable<Expression> CreateParameters(
            ConstructorInfo constructor,
            ParameterExpression services,
            Expression next)
        {
            return CreateParameters(
                constructor.GetParameters(),
                services,
                new Dictionary<Type, Expression>
                {
                    { typeof(QueryDelegate), next }
                });
        }

        private static IEnumerable<Expression> CreateParameters(
            IEnumerable<ParameterInfo> parameters,
            ParameterExpression services,
            IDictionary<Type, Expression> custom)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                if (custom.TryGetValue(parameter.ParameterType,
                    out Expression expression))
                {
                    yield return expression;
                }
                else
                {
                    yield return Expression.Convert(Expression.Call(
                        services,
                        typeof(IServiceProvider).GetMethod("GetService"),
                        Expression.Constant(parameter.ParameterType)),
                        parameter.ParameterType);
                }
            }
        }
    }
}
