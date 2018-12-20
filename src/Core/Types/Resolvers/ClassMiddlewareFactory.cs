using System;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers
{
    public static class ClassMiddlewareFactory
    {
        internal static FieldMiddleware Create<TMiddleware>()
            where TMiddleware : class
        {
            return next =>
            {
                var factory = MiddlewareActivator
                    .CompileFactory<TMiddleware, FieldDelegate>();

                return CreateDelegate<TMiddleware>(
                    (s, n) => factory(s, n),
                    next);
            };
        }

        internal static FieldMiddleware Create<TMiddleware>(
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            return next => CreateDelegate<TMiddleware>(factory, next);
        }

        internal static FieldDelegate CreateDelegate<TMiddleware>(
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory,
            FieldDelegate next)
            where TMiddleware : class
        {
            object sync = new object();
            TMiddleware middleware = null;

            var compiled = MiddlewareActivator
                .CompileMiddleware<TMiddleware, IMiddlewareContext>();

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
