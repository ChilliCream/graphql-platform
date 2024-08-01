using HotChocolate.Resolvers;

namespace HotChocolate;

public static partial class SchemaBuilderExtensions
{
    public static ISchemaBuilder Use<TMiddleware>(
        this ISchemaBuilder builder)
        where TMiddleware : class
    {
        return builder.Use(
            FieldClassMiddlewareFactory.Create<TMiddleware>());
    }

    public static ISchemaBuilder Use<TMiddleware>(
        this ISchemaBuilder builder,
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        return builder.Use(
            FieldClassMiddlewareFactory.Create(factory));
    }

    public static ISchemaBuilder Map(
        this ISchemaBuilder builder,
        FieldReference fieldReference,
        FieldMiddleware middleware)
    {
        return builder.Use(
            FieldClassMiddlewareFactory.Create(
                (s, n) => new MapMiddleware(
                    n, fieldReference, middleware(n))));
    }

    public static ISchemaBuilder Map<TMiddleware>(
        this ISchemaBuilder builder,
        FieldReference fieldReference)
        where TMiddleware : class
    {
        return builder.Use(
            FieldClassMiddlewareFactory.Create(
                (s, n) =>
                {
                    var classMiddleware =
                        FieldClassMiddlewareFactory.Create<TMiddleware>();
                    return new MapMiddleware(
                        n, fieldReference, classMiddleware(n));
                }));
    }

    public static ISchemaBuilder Map<TMiddleware>(
        this ISchemaBuilder builder,
        FieldReference fieldReference,
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        return builder.Use(
            FieldClassMiddlewareFactory.Create(
                (s, n) =>
                {
                    var classMiddleware =
                        FieldClassMiddlewareFactory
                            .Create(factory);
                    return new MapMiddleware(
                        n, fieldReference, classMiddleware(n));
                }));
    }
}
