using System;
using System.Collections.Generic;
using System.Linq;
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
        internal static MiddlewareFactory<TMiddleware, TRequestDelegate> CompileFactory<TMiddleware, TRequestDelegate>()
        {
            var services = Expression.Parameter(typeof(IServiceProvider));
            var nextDelegate = Expression.Parameter(typeof(TRequestDelegate));

            NewExpression createInstance = CreateMiddleware<TRequestDelegate>(
                typeof(TMiddleware).GetTypeInfo(), services, nextDelegate);

            return Expression.Lambda<MiddlewareFactory<TMiddleware, TRequestDelegate>>(
                createInstance, services, nextDelegate)
                .Compile();
        }

        internal static ClassQueryDelegate<TMiddleware, TContext> CompileMiddleware<TMiddleware, TContext>()
        {
            TypeInfo middlewareType = typeof(TMiddleware).GetTypeInfo();
            MethodInfo method = middlewareType.GetDeclaredMethod("InvokeAsync")
                ?? middlewareType.GetDeclaredMethod("Invoke");

            if (method == null)
            {
                // TODO : Resources
                throw new NotSupportedException(
                    "The provided middleware type must contain " +
                    "an invoke method.");
            }

            var context = Expression.Parameter(typeof(TContext));
            var services = Expression.Parameter(typeof(IServiceProvider));
            var middlewareInstance = Expression.Parameter(
                middlewareType.AsType());

            var middlewareCall = Expression.Call(
                middlewareInstance,
                method,
                CreateParameters<TContext>(method, services, context));

            return Expression.Lambda<ClassQueryDelegate<TMiddleware, TContext>>(
                middlewareCall, context, services, middlewareInstance)
                .Compile();
        }

        private static NewExpression CreateMiddleware<TRequestDelegate>(
            TypeInfo middleware,
            ParameterExpression services,
            Expression next)
        {
            ConstructorInfo constructor = CreateConstructor(middleware);
            IEnumerable<Expression> arguments =
                CreateParameters<TRequestDelegate>(constructor, services, next);
            return Expression.New(constructor, arguments);
        }

        private static ConstructorInfo CreateConstructor(TypeInfo middleware)
        {
            var constructors = middleware.DeclaredConstructors
                .Where(t => t.IsPublic).ToArray();
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
            MethodInfo getService = typeof(IServiceProvider)
                .GetTypeInfo()
                .GetDeclaredMethod("GetService");

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
                        getService,
                        Expression.Constant(parameter.ParameterType)),
                        parameter.ParameterType);
                }
            }
        }
    }
}
