namespace HotChocolate.Caching;

public static class SchemaBuilderExtensions
{
    public static ISchemaBuilder AddCacheControlDirectiveType(
        this ISchemaBuilder builder)
        => builder.AddDirectiveType<CacheControlDirectiveType>();

    public static ISchemaBuilder AddCacheControlScopeType(
        this ISchemaBuilder builder)
        => builder.AddType<CacheControlScopeType>();

    public static ISchemaBuilder AddCacheControl(
        this ISchemaBuilder builder)
    {
        return builder
            .AddCacheControlScopeType()
            .AddCacheControlDirectiveType()
            .TryAddTypeInterceptor<CacheControlTypeInterceptor>();
    }
}