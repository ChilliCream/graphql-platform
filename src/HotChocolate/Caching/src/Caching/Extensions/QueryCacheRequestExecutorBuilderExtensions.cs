using HotChocolate;
using HotChocolate.Caching;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IRequestExecutorBuilder"/>
/// to configure cache control support, including the query cache middleware,
/// cache control directive types, and default cache control options.
/// </summary>
public static class QueryCacheRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Registers the <see cref="QueryCacheMiddleware"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="after">
    /// The middleware key after which to insert the query cache middleware.
    /// Defaults to the timeout middleware.
    /// </param>
    public static IRequestExecutorBuilder UseQueryCache(
        this IRequestExecutorBuilder builder,
        string? after = null)
        => builder.UseRequest(
            QueryCacheMiddleware.Create(),
            after: after ?? WellKnownRequestMiddleware.TimeoutMiddleware);

    /// <summary>
    /// Adds cache control types, the constraints optimizer, and the default
    /// cache control type interceptor to the request executor.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    public static IRequestExecutorBuilder AddCacheControl(
        this IRequestExecutorBuilder builder)
    {
        builder.AddOperationCompilerOptimizer<CacheControlConstraintsOptimizer>();

        builder.ConfigureSchemaServices(
            services =>
            {
                services.AddOptions();
                services.AddSingleton<ICacheControlOptionsAccessor, CacheControlOptionsAccessor>();
            });

        return builder.ConfigureSchema(
            b =>
            {
                b.AddCacheControl();
                b.TryAddTypeInterceptor<CacheControlTypeInterceptor>();
            });
    }

    /// <summary>
    /// Modify the <see cref="CacheControlOptions"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="modifyOptions">
    /// Configure the <see cref="CacheControlOptions"/>.
    /// </param>
    public static IRequestExecutorBuilder ModifyCacheControlOptions(
        this IRequestExecutorBuilder builder,
        Action<CacheControlOptions> modifyOptions)
    {
        ArgumentNullException.ThrowIfNull(modifyOptions);

        builder.ConfigureSchemaServices(services => services.Configure(modifyOptions));

        return builder;
    }
}
