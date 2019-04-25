using System;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    public static class MiddlewareConfigurationExtensions
    {
        public static ISchemaConfiguration Use<TMiddleware>(
            this ISchemaConfiguration configuration)
            where TMiddleware : class
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create<TMiddleware>());
        }

        public static ISchemaConfiguration Use<TMiddleware>(
            this ISchemaConfiguration configuration,
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create(factory));
        }

        public static ISchemaConfiguration Map(
            this ISchemaConfiguration configuration,
            FieldReference fieldReference,
            FieldMiddleware middleware)
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create(
                    (s, n) => new MapMiddleware(
                        n, fieldReference, middleware(n))));
        }


        public static ISchemaConfiguration Map<TMiddleware>(
            this ISchemaConfiguration configuration,
            FieldReference fieldReference)
            where TMiddleware : class
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create(
                    (s, n) =>
                    {
                        FieldMiddleware classMiddleware =
                            FieldClassMiddlewareFactory.Create<TMiddleware>();
                        return new MapMiddleware(
                            n, fieldReference, classMiddleware(n));
                    }));
        }

        public static ISchemaConfiguration Map<TMiddleware>(
            this ISchemaConfiguration configuration,
            FieldReference fieldReference,
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create(
                    (s, n) =>
                    {
                        FieldMiddleware classMiddleware =
                            FieldClassMiddlewareFactory
                                .Create(factory);
                        return new MapMiddleware(
                            n, fieldReference, classMiddleware(n));
                    }));
        }
    }
}
