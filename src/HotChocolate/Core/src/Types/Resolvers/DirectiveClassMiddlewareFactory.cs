using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers;

public static class DirectiveClassMiddlewareFactory
{
    private static readonly MethodInfo _createGeneric =
        typeof(DirectiveClassMiddlewareFactory)
            .GetTypeInfo().DeclaredMethods.First(t =>
            {
                if (t.Name.EqualsOrdinal(nameof(Create)) && t.GetGenericArguments().Length == 1)
                {
                    return t.GetParameters().Length == 0;
                }
                return false;
            });

    private static readonly PropertyInfo _services =
        typeof(IResolverContext).GetProperty(nameof(IResolverContext.Services));

    internal static DirectiveMiddleware Create<TMiddleware>()
        where TMiddleware : class
    {
        return next =>
        {
            var factory =
                MiddlewareCompiler<TMiddleware>
                    .CompileFactory<IServiceProvider, FieldDelegate>(
                        (services, _) =>
                        new IParameterHandler[] { new ServiceParameterHandler(services) });

            return CreateDelegate(
                (s, n) => factory(s, n),
                next);
        };
    }

    internal static DirectiveMiddleware Create(Type middlewareType)
    {
        return (DirectiveMiddleware)_createGeneric
            .MakeGenericMethod(middlewareType)
            .Invoke(null, Array.Empty<object>());
    }

    internal static DirectiveMiddleware Create<TMiddleware>(
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        return next => CreateDelegate(factory, next);
    }

    internal static DirectiveDelegate CreateDelegate<TMiddleware>(
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory,
        FieldDelegate next)
        where TMiddleware : class
    {
        var sync = new object();
        TMiddleware middleware = null;

        var compiled =
            MiddlewareCompiler<TMiddleware>.CompileDelegate<IDirectiveContext>(
                (context, _) => new List<IParameterHandler>
                {
                    new ServiceParameterHandler(Expression.Property(context, _services))
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
