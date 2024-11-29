using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities.Properties;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace HotChocolate.Utilities;

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
internal static class MiddlewareCompiler<[DynamicallyAccessedMembers(PublicConstructors | PublicMethods)] TMiddleware>
{
    private static readonly MethodInfo _awaitHelper =
        typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.AwaitTaskHelper))!;

    internal static MiddlewareFactory<TMiddleware, TContext, TNext> CompileFactory<TContext, TNext>(
        CreateFactoryHandlers? createParameters = null)
    {
        var type = typeof(TMiddleware);
        var context = Expression.Parameter(typeof(TContext), "context");
        var next = Expression.Parameter(typeof(TNext), "next");

        var handlers = new List<IParameterHandler>();
        handlers.Add(new TypeParameterHandler(typeof(TNext), next));
        if (createParameters is not null)
        {
            handlers.AddRange(createParameters(context, next));
        }

        var createInstance = CreateMiddleware(type, handlers);

        return Expression
            .Lambda<MiddlewareFactory<TMiddleware, TContext, TNext>>(
                createInstance, context, next)
            .Compile();
    }

    internal static ClassQueryDelegate<TMiddleware, TContext> CompileDelegate<TContext>(
        CreateDelegateHandlers? createParameters = null)
    {
        var middlewareType = typeof(TMiddleware);
        var method = GetInvokeMethod(middlewareType);

        if (method == null)
        {
            throw new NotSupportedException(
                UtilityResources.MiddlewareActivator_NoInvokeMethod);
        }

        var context = Expression.Parameter(typeof(TContext));
        var middleware = Expression.Parameter(middlewareType);

        var handlers = new List<IParameterHandler>();
        handlers.Add(new TypeParameterHandler(typeof(TContext), context));
        if (createParameters is { })
        {
            handlers.AddRange(createParameters(context, middleware));
        }

        var arguments = CreateParameters(method.GetParameters(), handlers);
        var middlewareCall = CreateInvokeMethodCall(middleware, method, arguments);

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
        [DynamicallyAccessedMembers(PublicConstructors)] Type middleware,
        IReadOnlyList<IParameterHandler> parameterHandlers)
    {
        var constructor = CreateConstructor(middleware);
        var arguments = CreateParameters(
            constructor.GetParameters(), parameterHandlers);
        return Expression.New(constructor, arguments);
    }

    private static ConstructorInfo CreateConstructor(
        [DynamicallyAccessedMembers(PublicConstructors)] Type middleware)
    {
        var constructor =
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

        foreach (var parameter in parameters)
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

    private static MethodInfo? GetInvokeMethod(
        [DynamicallyAccessedMembers(PublicMethods)] Type middlewareType)
        => middlewareType.GetMethod("InvokeAsync") ?? middlewareType.GetMethod("Invoke");

    private static class ExpressionHelper
    {
        public static async ValueTask AwaitTaskHelper(Task task)
            => await task.ConfigureAwait(false);
    }
}
