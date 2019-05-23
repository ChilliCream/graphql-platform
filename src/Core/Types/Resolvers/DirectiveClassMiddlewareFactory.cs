using System;
using System.Linq;
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

        internal static DirectiveMiddleware Create<TMiddleware>()
            where TMiddleware : class
        {
            return next =>
            {
                MiddlewareFactory<TMiddleware, FieldDelegate> factory =
                    MiddlewareActivator
                        .CompileFactory<TMiddleware, FieldDelegate>();

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
                MiddlewareActivator
                    .CompileMiddleware<TMiddleware, IDirectiveContext>();

            return context =>
            {
                if (middleware == null)
                {
                    lock (sync)
                    {
                        if (middleware == null)
                        {
                            middleware = factory(
                                context.Service<IServiceProvider>(),
                                next);
                        }
                    }
                }

                return compiled(
                    context,
                    context.Service<IServiceProvider>(),
                    middleware);
            };
        }
    }
}
