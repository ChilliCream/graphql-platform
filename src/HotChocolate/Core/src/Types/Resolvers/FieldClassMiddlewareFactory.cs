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
        private static readonly MethodInfo _createGeneric =
            typeof(FieldClassMiddlewareFactory)
            .GetTypeInfo().DeclaredMethods.First(t =>
            {
                return t.Name.EqualsOrdinal(nameof(FieldClassMiddlewareFactory.Create)) &&
                    t.IsGenericMethod;
            });

        private static readonly PropertyInfo _services =
            typeof(IResolverContext).GetProperty(nameof(IResolverContext.Services));

        public static FieldMiddleware Create<TMiddleware>(params object[] services)
            where TMiddleware : class
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var parameters = new List<IParameterHandler>();

            foreach (object service in services)
            {
                if (service is { })
                {
                    parameters.Add(new TypeParameterHandler(
                        service.GetType(), 
                        Expression.Constant(service)));
                }
            }

            return next =>
            {
                MiddlewareFactory<TMiddleware, IServiceProvider, FieldDelegate> factory =
                    MiddlewareCompiler<TMiddleware>.CompileFactory<IServiceProvider, FieldDelegate>(
                        (services, next) =>
                        {
                            parameters.Add(new ServiceParameterHandler(services));
                            return parameters;
                        });

                return CreateDelegate((s, n) => factory(s, n), next);
            };
        }

        public static FieldMiddleware Create(Type middlewareType, params object[] services)
        {
            return (FieldMiddleware)_createGeneric
                .MakeGenericMethod(middlewareType)
                .Invoke(null, new object[] { services });
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
            object sync = new object();
            TMiddleware middleware = null;

            ClassQueryDelegate<TMiddleware, IMiddlewareContext> compiled =
                MiddlewareCompiler<TMiddleware>.CompileDelegate<IMiddlewareContext>(
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
