using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Utilities.Properties;

namespace HotChocolate.Utilities
{
    internal delegate TMiddleware MiddlewareFactory<out TMiddleware, in TContext, in TNext>(
        TContext context,
        TNext next);

    internal delegate ValueTask ClassQueryDelegate<in TMiddleware, in TContext>(
        TContext context,
        TMiddleware middleware);

    internal delegate IEnumerable<IParameterHandler> CreateFactoryHandlers(
        ParameterExpression context,
        ParameterExpression next);

    internal delegate IEnumerable<IParameterHandler> CreateDelegateHandlers(
        ParameterExpression context,
        ParameterExpression middleware);

    /// <summary>
    /// This helper compiles classes to middleware delegates.
    /// </summary>
    internal static class MiddlewareCompiler<TMiddleware>
    {
        private static readonly MethodInfo _awaitHelper =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.AwaitTaskHelper))!;

        internal static MiddlewareFactory<TMiddleware, TContext, TNext> CompileFactory<TContext, TNext>(
            CreateFactoryHandlers? createParameters = null)
        {
            Type type = typeof(TMiddleware);
            ParameterExpression context = Expression.Parameter(typeof(TContext), "context");
            ParameterExpression next = Expression.Parameter(typeof(TNext), "next");

            var handlers = new List<IParameterHandler>();
            handlers.Add(new TypeParameterHandler(typeof(TNext), next));
            if (createParameters is { })
            {
                handlers.AddRange(createParameters(context, next));
            }

            NewExpression createInstance = CreateMiddleware(type, handlers);

            return Expression
                .Lambda<MiddlewareFactory<TMiddleware, TContext, TNext>>(
                    createInstance, context, next)
                .Compile();
        }

        internal static ClassQueryDelegate<TMiddleware, TContext> CompileDelegate<TContext>(
            CreateDelegateHandlers? createParameters = null)
        {
            Type middlewareType = typeof(TMiddleware);
            MethodInfo? method = GetInvokeMethod(middlewareType);

            if (method == null)
            {
                throw new NotSupportedException(
                    UtilityResources.MiddlewareActivator_NoInvokeMethod);
            }

            ParameterExpression context = Expression.Parameter(typeof(TContext));
            ParameterExpression middleware = Expression.Parameter(middlewareType);

            var handlers = new List<IParameterHandler>();
            handlers.Add(new TypeParameterHandler(typeof(TContext), context));
            if (createParameters is { })
            {
                handlers.AddRange(createParameters(context, middleware));
            }

            List<Expression> arguments =
                CreateParameters(method.GetParameters(), handlers);

            MethodCallExpression? middlewareCall =
                CreateInvokeMethodCall(middleware, method, arguments);

            return Expression.Lambda<ClassQueryDelegate<TMiddleware, TContext>>(
                middlewareCall, context, middleware)
                .Compile();
        }

        private static MethodCallExpression CreateInvokeMethodCall(
            ParameterExpression middleware,
            MethodInfo method,
            IReadOnlyList<Expression> arguments)
        {
            if (method.ReturnType == typeof(Task))
            {
                return Expression.Call(_awaitHelper,
                    Expression.Call(middleware, method, arguments));
            }

            if (method.ReturnType == typeof(ValueTask))
            {
                return Expression.Call(middleware, method, arguments);
            }

            throw new NotSupportedException(
                UtilityResources.MiddlewareCompiler_ReturnTypeNotSupported);
        }

        private static NewExpression CreateMiddleware(
            Type middleware,
            IReadOnlyList<IParameterHandler> parameterHandlers)
        {
            ConstructorInfo constructor = CreateConstructor(middleware);
            List<Expression> arguments = CreateParameters(
                constructor.GetParameters(), parameterHandlers);
            return Expression.New(constructor, arguments);
        }

        private static ConstructorInfo CreateConstructor(Type middleware)
        {
            ConstructorInfo? constructor =
                middleware.GetConstructors().SingleOrDefault(t => t.IsPublic);

            if (constructor is null)
            {
                throw new NotSupportedException(
                    UtilityResources.MiddlewareActivator_OneConstructor);
            }

            return constructor;
        }

        private static List<Expression> CreateParameters(
            IEnumerable<ParameterInfo> parameters,
            IReadOnlyList<IParameterHandler> parameterHandlers)
        {
            var arguments = new List<Expression>();

            foreach (ParameterInfo parameter in parameters)
            {
                if (parameterHandlers.FirstOrDefault(t => t.CanHandle(parameter)) is { } h)
                {
                    arguments.Add(h.CreateExpression(parameter));
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            UtilityResources.MiddlewareActivator_ParameterNotSupported,
                            parameter.Name));
                }
            }

            return arguments;
        }

        private static MethodInfo? GetInvokeMethod(Type middlewareType) =>
            middlewareType.GetMethod("InvokeAsync") ??
            middlewareType.GetMethod("Invoke");

        private static class ExpressionHelper
        {
            public static async ValueTask AwaitTaskHelper(Task task) =>
                await task.ConfigureAwait(false);
        }
    }
}
