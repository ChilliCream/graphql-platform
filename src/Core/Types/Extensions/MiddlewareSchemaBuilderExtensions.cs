using System;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    public static class MiddlewareSchemaBuilderExtensions
    {
        public static ISchemaBuilder Use<TMiddleware>(
            this ISchemaBuilder configuration)
            where TMiddleware : class
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create<TMiddleware>());
        }

        public static ISchemaBuilder Use<TMiddleware>(
            this ISchemaBuilder configuration,
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create(factory));
        }

        public static ISchemaBuilder Map(
            this ISchemaBuilder configuration,
            FieldReference fieldReference,
            FieldMiddleware middleware)
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create(
                    (s, n) => new MapMiddleware(
                        n, fieldReference, middleware(n))));
        }


        public static ISchemaBuilder Map<TMiddleware>(
            this ISchemaBuilder configuration,
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

        public static ISchemaBuilder Map<TMiddleware>(
            this ISchemaBuilder configuration,
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
