#nullable disable

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers;

public static class FieldClassMiddlewareFactory
{
    private static readonly MethodInfo s_createGeneric =
        typeof(FieldClassMiddlewareFactory)
        .GetTypeInfo().DeclaredMethods.First(t =>
            t.Name.EqualsOrdinal(nameof(Create))
            && t.IsGenericMethod);

    private static readonly PropertyInfo s_services =
        typeof(IResolverContext).GetProperty(nameof(IResolverContext.Services));

    public static FieldMiddleware Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        params (Type Service, object Instance)[] services)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(services);

        return next =>
        {
            var parameters = new List<IParameterHandler>();

            foreach (var service in services)
            {
                parameters.Add(new TypeParameterHandler(
                    service.Service,
                    Expression.Constant(service.Instance)));
            }

            var factory =
                MiddlewareCompiler<TMiddleware>.CompileFactory<IServiceProvider, FieldDelegate>(
                    (sp, _) =>
                    {
                        parameters.Add(new ServiceParameterHandler(sp));
                        return parameters;
                    });

            return CreateDelegate((s, n) => factory(s, n), next);
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
    public static FieldMiddleware Create(
        Type middlewareType,
        params (Type Service, object Instance)[] services)
    {
        return (FieldMiddleware)s_createGeneric
            .MakeGenericMethod(middlewareType)
            .Invoke(null, [services]);
    }

    public static FieldMiddleware Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return next => CreateDelegate(factory, next);
    }

    private static FieldDelegate CreateDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory,
        FieldDelegate next)
        where TMiddleware : class
    {
        var sync = new object();
        TMiddleware middleware = null;

        var compiled =
            MiddlewareCompiler<TMiddleware>.CompileDelegate<IMiddlewareContext>(
                (context, _) => new List<IParameterHandler>
                {
                    new ServiceParameterHandler(Expression.Property(context, s_services))
                });

        return context =>
        {
            if (middleware is null)
            {
                lock (sync)
                {
                    middleware ??= factory(context.Services, next);
                }
            }

            return compiled(context, middleware);
        };
    }
}
