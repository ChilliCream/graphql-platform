using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers;

public static class FieldClassMiddlewareFactory
{
    private static readonly MethodInfo _createGeneric =
        typeof(FieldClassMiddlewareFactory)
        .GetTypeInfo().DeclaredMethods.First(t =>
            t.Name.EqualsOrdinal(nameof(Create)) &&
            t.IsGenericMethod);

    private static readonly PropertyInfo _services =
        typeof(IResolverContext).GetProperty(nameof(IResolverContext.Services));

    public static FieldMiddleware Create<TMiddleware>(
        params (Type Service, object Instance)[] services)
        where TMiddleware : class
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

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

    public static FieldMiddleware Create(
        Type middlewareType,
        params (Type Service, object Instance)[] services)
    {
        return (FieldMiddleware)_createGeneric
            .MakeGenericMethod(middlewareType)
            .Invoke(null, [services,]);
    }

    public static FieldMiddleware Create<TMiddleware>(
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return next => CreateDelegate(factory, next);
    }

    private static FieldDelegate CreateDelegate<TMiddleware>(
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
                    new ServiceParameterHandler(Expression.Property(context, _services)),
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
