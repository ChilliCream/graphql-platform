using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers
{
    public static class DirectiveClassMiddlewareFactory
    {
        private static MethodInfo _createGeneric =
            typeof(DirectiveClassMiddlewareFactory)
            .GetTypeInfo().DeclaredMethods.First(t =>
            {
                if (t.Name.EqualsOrdinal(
                    nameof(DirectiveClassMiddlewareFactory.Create))
                    && t.GetGenericArguments().Length == 1)
                {
                    return t.GetParameters().Length == 0;
                }
                return false;
            });

        private static PropertyInfo _services =
            typeof(IResolverContext).GetProperty(nameof(IResolverContext.Services));

        internal static DirectiveMiddleware Create<TMiddleware>()
            where TMiddleware : class
        {
            return next =>
            {
                MiddlewareFactory<TMiddleware, IServiceProvider, FieldDelegate> factory =
                    MiddlewareCompiler<TMiddleware>
                        .CompileFactory<IServiceProvider, FieldDelegate>(
                            (services, next) =>
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
            object sync = new object();
            TMiddleware middleware = null;

            ClassQueryDelegate<TMiddleware, IDirectiveContext> compiled =
                MiddlewareCompiler<TMiddleware>.CompileDelegate<IDirectiveContext>(
                    (context, middleware) => new List<IParameterHandler>
                    {
                        new ServiceParameterHandler(Expression.Property(context, _services))
                    });

            return context =>
            {
                if (middleware is null)
                {
                    lock (sync)
                    {
                        middleware = middleware ?? factory(context.Services, next);
                    }
                }

                return compiled(context, middleware);
            };
        }
    }
}
