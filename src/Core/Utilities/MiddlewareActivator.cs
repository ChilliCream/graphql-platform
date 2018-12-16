using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace HotChocolate.Utilities
{
    internal delegate T MiddlewareFactory<T, TRequestDelegate>(
        IServiceProvider services,
        TRequestDelegate next);

    internal delegate Task ClassQueryDelegate<T, TContext>(
        TContext context,
        IServiceProvider services,
        T middleware);

    internal static class MiddlewareActivator
    {
        internal static MiddlewareFactory<T, TRequestDelegate> CompileFactory<T, TRequestDelegate>()
        {
            var services = Expression.Parameter(typeof(IServiceProvider));
            var nextDelegate = Expression.Parameter(typeof(TRequestDelegate));

            NewExpression createInstance = CreateMiddleware<TRequestDelegate>(
                typeof(T), services, nextDelegate);

            return Expression.Lambda<MiddlewareFactory<T, TRequestDelegate>>(
                createInstance, services, nextDelegate)
                .Compile();
        }

        internal static ClassQueryDelegate<T, TContext> CompileMiddleware<T, TContext>()
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

            var context = Expression.Parameter(typeof(TContext));
            var services = Expression.Parameter(typeof(IServiceProvider));
            var middlewareInstance = Expression.Parameter(middlewareType);

            var middlewareCall = Expression.Call(
                middlewareInstance,
                method,
                CreateParameters<TContext>(method, services, context));

            return Expression.Lambda<ClassQueryDelegate<T, TContext>>(
                middlewareCall, context, services, middlewareInstance)
                .Compile();
        }

        private static NewExpression CreateMiddleware<TRequestDelegate>(
            Type middleware,
            ParameterExpression services,
            Expression next)
        {
            ConstructorInfo constructor = CreateConstructor(middleware);
            IEnumerable<Expression> arguments =
                CreateParameters<TRequestDelegate>(constructor, services, next);
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

        private static IEnumerable<Expression> CreateParameters<TContext>(
            MethodInfo invokeMethod,
            ParameterExpression services,
            Expression context)
        {
            return CreateParameters(
                invokeMethod.GetParameters(),
                services,
                new Dictionary<Type, Expression>
                {
                    { typeof(TContext), context }
                });
        }

        private static IEnumerable<Expression> CreateParameters<TRequestDelegate>(
            ConstructorInfo constructor,
            ParameterExpression services,
            Expression next)
        {
            return CreateParameters(
                constructor.GetParameters(),
                services,
                new Dictionary<Type, Expression>
                {
                    { typeof(TRequestDelegate), next }
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
