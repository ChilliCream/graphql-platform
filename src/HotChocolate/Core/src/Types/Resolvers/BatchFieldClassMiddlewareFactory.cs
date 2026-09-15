using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers;

internal static class BatchFieldClassMiddlewareFactory
{
    private static readonly MethodInfo s_createGeneric =
        typeof(BatchFieldClassMiddlewareFactory).GetTypeInfo().DeclaredMethods.First(
            t => t.Name.EqualsOrdinal(nameof(Create))
                && t.IsGenericMethod
                && t.GetParameters()[0].ParameterType.IsArray);

    private static readonly PropertyInfo s_services =
        typeof(IResolverContext).GetProperty(nameof(IResolverContext.Services))!;

    private static readonly PropertyInfo s_item =
        typeof(ImmutableArray<IMiddlewareContext>).GetProperty("Item")!;

    internal static BatchFieldMiddleware Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        params (Type Service, object Instance)[] services)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(services);

        return next =>
        {
            var factory = MiddlewareCompiler<TMiddleware>
                .CompileFactory<IServiceProvider, BatchFieldDelegate>((sp, _) =>
                {
                    var parameters = new List<IParameterHandler>();
                    foreach (var service in services)
                    {
                        parameters.Add(new TypeParameterHandler(
                            service.Service,
                            Expression.Constant(service.Instance)));
                    }

                    parameters.Add(new ServiceParameterHandler(sp));
                    return parameters;
                });

            return CreateDelegate((s, n) => factory(s, n), next);
        };
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060",
        Justification = "The middleware type supplies the generic factory constraints at runtime.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Runtime generic factory activation is used in JIT-compatible environments.")]
    internal static BatchFieldMiddleware Create(
        Type middlewareType,
        params (Type Service, object Instance)[] services)
        => (BatchFieldMiddleware)s_createGeneric
            .MakeGenericMethod(middlewareType)
            .Invoke(null, [services])!;

    internal static BatchFieldMiddleware Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        Func<IServiceProvider, BatchFieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return next => CreateDelegate(factory, next);
    }

    internal static Expression CreateServicesExpression(Expression contexts)
        => Expression.Property(
            Expression.Property(contexts, s_item, Expression.Constant(0)),
            s_services);

    private static BatchFieldDelegate CreateDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        Func<IServiceProvider, BatchFieldDelegate, TMiddleware> factory,
        BatchFieldDelegate next)
        where TMiddleware : class
    {
        var sync = new object();
        TMiddleware? middleware = null;
        var compiled = MiddlewareCompiler<TMiddleware>
            .CompileDelegate<ImmutableArray<IMiddlewareContext>>((contexts, _) =>
                [new ServiceParameterHandler(CreateServicesExpression(contexts))]);

        return contexts =>
        {
            if (contexts.IsDefaultOrEmpty)
            {
                return ValueTask.CompletedTask;
            }

            if (middleware is null)
            {
                lock (sync)
                {
                    middleware ??= factory(contexts[0].Services, next);
                }
            }

            return compiled(contexts, middleware);
        };
    }
}
