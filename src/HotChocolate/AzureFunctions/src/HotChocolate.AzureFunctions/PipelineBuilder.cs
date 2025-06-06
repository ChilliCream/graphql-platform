using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.AspNetCore;
using HotChocolate.Utilities;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AzureFunctions;

internal sealed class PipelineBuilder
{
    private static readonly ParameterExpression s_context =
        Expression.Parameter(typeof(HttpContext), "context");
    private static readonly ParameterExpression s_services =
        Expression.Parameter(typeof(IServiceProvider), "services");
    private static readonly ParameterExpression s_next =
        Expression.Parameter(typeof(RequestDelegate), "next");
    private static readonly ConstantExpression s_schemaName =
        Expression.Constant(ISchemaDefinition.DefaultName, typeof(string));
    private static readonly ConstantExpression s_routing =
        Expression.Constant(MiddlewareRoutingType.Integrated, typeof(MiddlewareRoutingType));
    private static readonly MethodInfo s_getService =
        typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService))!;
    private static readonly MethodInfo s_compileInvoke =
        typeof(PipelineBuilder).GetMethod(
            nameof(CompileInvoke),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly List<(Type, object[])> _components = [];

    public PipelineBuilder UseMiddleware<T>(params object[] args)
    {
        _components.Add((typeof(T), args));
        return this;
    }

    public RequestDelegate Compile(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (_components.Count == 0)
        {
            throw new InvalidOperationException(
                "There must be at least one component in order to build a pipeline.");
        }

        RequestDelegate pipeline = context =>
        {
            context.Response.StatusCode = 404;
            return Task.CompletedTask;
        };

        for (var i = _components.Count - 1; i >= 0; i--)
        {
            (Type Type, object[] Args) component = _components[i];
            var constructors = component.Type.GetConstructors();

            if (constructors.Length != 1)
            {
                throw new InvalidOperationException(
                    "A middleware must have a single public constructor.");
            }

            var invokeMethod = TryResolveInvoke(component.Type);

            if (invokeMethod is null)
            {
                throw new InvalidOperationException(
                    "A middleware must have a public method InvokeAsync that returns " +
                    "`Task` and has a single parameter `HttpContext`.");
            }

            var next = CompileFactory(constructors[0], invokeMethod, component.Args);
            pipeline = next(services, pipeline);
        }

        return pipeline;
    }

    private Func<IServiceProvider, RequestDelegate, RequestDelegate> CompileFactory(
        ConstructorInfo constructor,
        MethodInfo invokeMethod,
        object[] args)
    {
        var list = new List<Expression>();

        foreach (var parameter in constructor.GetParameters())
        {
            if (parameter.ParameterType == typeof(RequestDelegate))
            {
                list.Add(s_next);
            }
            else if (parameter.ParameterType == typeof(IServiceProvider))
            {
                list.Add(s_services);
            }
            else if (parameter.Name.EqualsOrdinal("schemaName") &&
                parameter.ParameterType == typeof(string))
            {
                list.Add(s_schemaName);
            }
            else if (parameter.ParameterType == typeof(MiddlewareRoutingType))
            {
                list.Add(s_routing);
            }
            else
            {
                var resolved = false;

                if (args.Length > 0)
                {
                    foreach (var arg in args)
                    {
                        if (parameter.ParameterType.IsInstanceOfType(arg))
                        {
                            list.Add(Expression.Convert(
                                Expression.Constant(arg),
                                parameter.ParameterType));
                            resolved = true;
                            break;
                        }
                    }
                }

                if (!resolved)
                {
                    Expression parameterType = Expression.Constant(parameter.ParameterType);
                    list.Add(Expression.Convert(
                        Expression.Call(s_services, s_getService, parameterType),
                        parameter.ParameterType));
                }
            }
        }

        Expression instance = Expression.New(constructor, list);
        Expression invoke = Expression.Constant(invokeMethod);
        Expression requestDelegate = Expression.Call(s_compileInvoke, instance, invoke);

        return Expression.Lambda<Func<IServiceProvider, RequestDelegate, RequestDelegate>>(
            requestDelegate, s_services, s_next)
            .Compile();
    }

    private static RequestDelegate CompileInvoke(object obj, MethodInfo invokeMethod)
    {
        Expression instance = Expression.Constant(obj);
        var invoke = Expression.Call(instance, invokeMethod, s_context);
        var lambda = Expression.Lambda<RequestDelegate>(invoke, s_context);
        return lambda.Compile();
    }

    private MethodInfo? TryResolveInvoke(Type type)
        => type.GetMethod("InvokeAsync") ?? type.GetMethod("Invoke");
}
