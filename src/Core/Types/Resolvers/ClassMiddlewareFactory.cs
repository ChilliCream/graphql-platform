using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers
{
    public static class FieldClassMiddlewareFactory
    {
        private static MethodInfo _createGeneric =
            typeof(FieldClassMiddlewareFactory)
            .GetTypeInfo().DeclaredMethods.First(t =>
            {
                if (t.Name.EqualsOrdinal(
                    nameof(FieldClassMiddlewareFactory.Create))
                    && t.GetGenericArguments().Length == 1)
                {
                    var parameter = t.GetParameters();
                    return parameter.Length == 1 && parameter[0].Name == "customParameters";
                }
                return false;
            });

        public static FieldMiddleware Create<TMiddleware>(
            params object[] customParameters
            )
            where TMiddleware : class
        {
            return next =>
            {
                MiddlewareFactory<TMiddleware, FieldDelegate> factory =
                    MiddlewareActivator
                        .CompileFactory<TMiddleware, FieldDelegate>(customParameters);

                return CreateDelegate(
                    (s, n) => factory(s, n),
                    next);
            };
        }

        public static FieldMiddleware Create(
            Type middlewareType,
            params object[] customParameters)
        {
            return (FieldMiddleware)_createGeneric
                .MakeGenericMethod(middlewareType)
                .Invoke(null, new object[] { customParameters });
        }

        public static FieldMiddleware Create<TMiddleware>(
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            return next => CreateDelegate(factory, next);
        }

        private static FieldDelegate CreateDelegate<TMiddleware>(
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory,
            FieldDelegate next)
            where TMiddleware : class
        {
            object sync = new object();
            TMiddleware middleware = null;

            ClassQueryDelegate<TMiddleware, IMiddlewareContext> compiled =
                MiddlewareActivator
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
