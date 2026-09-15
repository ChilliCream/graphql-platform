using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers;

internal static class BatchDirectiveClassMiddlewareFactory
{
    private static readonly MethodInfo s_createGeneric =
        typeof(BatchDirectiveClassMiddlewareFactory).GetTypeInfo().DeclaredMethods.First(
            t => t.Name.EqualsOrdinal(nameof(Create))
                && t.IsGenericMethod
                && t.GetParameters().Length == 0);

    internal static BatchDirectiveMiddleware Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>()
        where TMiddleware : class
        => (next, directive) =>
        {
            var handler = new DirectiveParameterHandler(directive);
            var factory = MiddlewareCompiler<TMiddleware>
                .CompileFactory<IServiceProvider, BatchFieldDelegate>((services, _) =>
                    [handler, new ServiceParameterHandler(services)]);

            return CreateDelegate((s, n) => factory(s, n), next, handler);
        };

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060",
        Justification = "The middleware type supplies the generic factory constraints at runtime.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Runtime generic factory activation is used in JIT-compatible environments.")]
    internal static BatchDirectiveMiddleware Create(Type middlewareType)
        => (BatchDirectiveMiddleware)s_createGeneric
            .MakeGenericMethod(middlewareType)
            .Invoke(null, [])!;

    internal static BatchDirectiveMiddleware Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        Func<IServiceProvider, BatchFieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return (next, directive) => CreateDelegate(
            factory,
            next,
            new DirectiveParameterHandler(directive));
    }

    private static BatchFieldDelegate CreateDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        Func<IServiceProvider, BatchFieldDelegate, TMiddleware> factory,
        BatchFieldDelegate next,
        DirectiveParameterHandler handler)
        where TMiddleware : class
    {
        var sync = new object();
        TMiddleware? middleware = null;
        var compiled = MiddlewareCompiler<TMiddleware>
            .CompileDelegate<ImmutableArray<IMiddlewareContext>>((contexts, _) =>
                [handler, new ServiceParameterHandler(
                    BatchFieldClassMiddlewareFactory.CreateServicesExpression(contexts))]);

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
