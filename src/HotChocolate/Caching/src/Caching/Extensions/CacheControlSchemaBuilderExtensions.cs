namespace HotChocolate.Caching;

public static class CacheControlSchemaBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="CacheControlDirectiveType"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    public static ISchemaBuilder AddCacheControlDirectiveType(
        this ISchemaBuilder builder)
        => builder.AddDirectiveType<CacheControlDirectiveType>();

    /// <summary>
    /// Adds the <see cref="CacheControlScopeType"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    public static ISchemaBuilder AddCacheControlScopeType(
        this ISchemaBuilder builder)
        => builder.AddType<CacheControlScopeType>();

    /// <summary>
    /// Adds the CacheControl types.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    public static ISchemaBuilder AddCacheControl(
        this ISchemaBuilder builder)
    {
        return builder
            .AddCacheControlScopeType()
            .AddCacheControlDirectiveType()
            .TryAddTypeInterceptor<CacheControlValidationTypeInterceptor>();
    }
}
