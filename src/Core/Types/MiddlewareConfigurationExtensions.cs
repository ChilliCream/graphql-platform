using System;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    public static class MiddlewareConfigurationExtensions
    {
        public static IMiddlewareConfiguration Use<TMiddleware>(
            this IMiddlewareConfiguration configuration)
            where TMiddleware : class
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create<TMiddleware>());
        }

        public static IMiddlewareConfiguration Use<TMiddleware>(
            this IMiddlewareConfiguration configuration,
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create<TMiddleware>(factory));
        }

        public static IMiddlewareConfiguration Map(
            this IMiddlewareConfiguration configuration,
            FieldReference fieldReference,
            FieldMiddleware middleware)
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create<MapMiddleware>(
                    (s, n) => new MapMiddleware(
                        n, fieldReference, middleware(n))));
        }


        public static IMiddlewareConfiguration Map<TMiddleware>(
            this IMiddlewareConfiguration configuration,
            FieldReference fieldReference)
            where TMiddleware : class
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create<MapMiddleware>(
                    (s, n) =>
                    {
                        FieldMiddleware classMiddleware =
                            FieldClassMiddlewareFactory.Create<TMiddleware>();
                        return new MapMiddleware(
                            n, fieldReference, classMiddleware(n));
                    }));
        }

        public static IMiddlewareConfiguration Map<TMiddleware>(
            this IMiddlewareConfiguration configuration,
            FieldReference fieldReference,
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            return configuration.Use(
                FieldClassMiddlewareFactory.Create<MapMiddleware>(
                    (s, n) =>
                    {
                        FieldMiddleware classMiddleware =
                            FieldClassMiddlewareFactory.Create<TMiddleware>(factory);
                        return new MapMiddleware(
                            n, fieldReference, classMiddleware(n));
                    }));
        }
    }
}
