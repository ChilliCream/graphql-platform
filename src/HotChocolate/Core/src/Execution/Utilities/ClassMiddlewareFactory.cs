using System;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Utilities
{
    internal static class ClassMiddlewareFactory
    {
        internal static RequestMiddleware Create<TMiddleware>()
            where TMiddleware : class
        {
            return next =>
            {
                MiddlewareFactory<TMiddleware, RequestDelegate> factory =
                    MiddlewareActivator.CompileFactory<TMiddleware, RequestDelegate>();

                return CreateDelegate((s, n) => factory(s, n), next);
            };
        }

        internal static RequestMiddleware Create<TMiddleware>(
            Func<IServiceProvider, RequestDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            return next => CreateDelegate(factory, next);
        }

        internal static RequestDelegate CreateDelegate<TMiddleware>(
            Func<IServiceProvider, RequestDelegate, TMiddleware> factory,
            RequestDelegate next)
            where TMiddleware : class
        {
            object sync = new object();
            TMiddleware? middleware = null;

            ClassQueryDelegate<TMiddleware, IRequestContext> compiled =
                MiddlewareActivator.CompileMiddleware<TMiddleware, IRequestContext>();

            return context =>
            {
                if (middleware == null)
                {
                    lock (sync)
                    {
                        if (middleware == null)
                        {
                            middleware = factory(context.Services, next);
                        }
                    }
                }

                return compiled(context, context.Services, middleware);
            };
        }
    }
}
