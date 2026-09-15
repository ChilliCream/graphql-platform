using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers;

internal static class DirectiveClassMiddlewareFactory
{
    private static readonly MethodInfo s_createGeneric =
        typeof(DirectiveClassMiddlewareFactory)
            .GetTypeInfo().DeclaredMethods.First(
                t =>
                {
                    if (t.Name.EqualsOrdinal(nameof(Create)) && t.GetGenericArguments().Length == 1)
                    {
                        return t.GetParameters().Length == 0;
                    }
                    return false;
                });

    private static readonly PropertyInfo s_services =
        typeof(IResolverContext).GetProperty(nameof(IResolverContext.Services))!;

    internal static DirectiveMiddleware Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>()
        where TMiddleware : class
    {
        var sync = new object();
        MiddlewareFactory<TMiddleware, IServiceProvider, FieldDelegate>? activate = null;
        ClassQueryDelegate<TMiddleware, IMiddlewareContext>? invoke = null;

        return (next, directive) =>
        {
            if (invoke is null || activate is null)
            {
                lock (sync)
                {
                    if (invoke is null || activate is null)
                    {
                        var directiveHandler = new DirectiveParameterHandler(directive);

                        activate =
                            MiddlewareCompiler<TMiddleware>
                                .CompileFactory<IServiceProvider, FieldDelegate>(
                                    (services, _) => new IParameterHandler[]
                                    {
                                        directiveHandler, new ServiceParameterHandler(services)
                                    });

                        invoke =
                            MiddlewareCompiler<TMiddleware>
                                .CompileDelegate<IMiddlewareContext>(
                                    (context, _) => new List<IParameterHandler>
                                    {
                                        directiveHandler,
                                        new ServiceParameterHandler(
                                            Expression.Property(context, s_services))
                                    });
                    }
                }
            }

            TMiddleware? instance = null;

            return context =>
            {
                instance ??= activate(context.Services, next);
                return invoke(context, instance);
            };
        };
    }

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2060",
        Justification =
            "The Create<TMiddleware> method's generic constraints are satisfied at runtime by the middleware type.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification =
            "This method uses MakeGenericMethod to instantiate a generic factory method and is only used in "
            + "JIT-compatible environments.")]
    internal static DirectiveMiddleware Create(Type middlewareType)
        => (DirectiveMiddleware)s_createGeneric
            .MakeGenericMethod(middlewareType)
            .Invoke(null, [])!;

    internal static DirectiveMiddleware Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        Func<IServiceProvider, FieldDelegate, TMiddleware> activate)
        where TMiddleware : class
    {
        var sync = new object();
        ClassQueryDelegate<TMiddleware, IMiddlewareContext>? invoke = null;

        return (next, directive) =>
        {
            if (invoke is null)
            {
                lock (sync)
                {
                    if (invoke is null)
                    {
                        var directiveHandler = new DirectiveParameterHandler(directive);

                        invoke =
                            MiddlewareCompiler<TMiddleware>
                                .CompileDelegate<IMiddlewareContext>(
                                    (context, _) => new List<IParameterHandler>
                                    {
                                        directiveHandler,
                                        new ServiceParameterHandler(
                                            Expression.Property(context, s_services))
                                    });
                    }
                }
            }

            TMiddleware? instance = null;

            return context =>
            {
                instance ??= activate(context.Services, next);
                return invoke(context, instance);
            };
        };
    }
}
