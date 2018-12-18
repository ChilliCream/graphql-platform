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
                object sync = new object();
                TMiddleware middleware = null;

                var factory = MiddlewareActivator
                    .CompileFactory<TMiddleware, FieldDelegate>();
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
            };
        }
    }
}
