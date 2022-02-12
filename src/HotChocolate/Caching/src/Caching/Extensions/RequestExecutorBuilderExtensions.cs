using System;
using HotChocolate.Caching;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder UseQueryCache(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest<QueryCacheMiddleware>();

    public static IRequestExecutorBuilder UseQueryCachePipeline(this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder
            .UseInstrumentations()
            .UseExceptions()
            .UseTimeout()
            .UseQueryCache()
            .UseDocumentCache()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationCache()
            .UseOperationComplexityAnalyzer()
            .UseOperationResolver()
            .UseOperationVariableCoercion()
            .UseOperationExecution();
    }

    public static IRequestExecutorBuilder AddCacheControl(this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddCacheControl());
    }

    public static IRequestExecutorBuilder ModifyCacheControlOptions(this IRequestExecutorBuilder builder,
        Action<CacheControlOptions> modifyOptions)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton(modifyOptions);

        return builder;
    }

    public static IRequestExecutorBuilder AddQueryCache<TCache>(this IRequestExecutorBuilder builder)
        where TCache : class, IQueryCache
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IQueryCache, TCache>();

        return builder.AddQueryCacheInternals();
    }

    public static IRequestExecutorBuilder AddQueryCache<TCache>(this IRequestExecutorBuilder builder,
        Func<IServiceProvider, TCache> cacheFactory)
        where TCache : class, IQueryCache
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IQueryCache>(cacheFactory);

        return builder.AddQueryCacheInternals();
    }

    private static IRequestExecutorBuilder AddQueryCacheInternals(this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<ICacheControlOptionsAccessor>(sp =>
        {
            var accessor = new CacheControlOptionsAccessor();

            foreach (Action<CacheControlOptions> configure in sp.GetServices<Action<CacheControlOptions>>())
            {
                configure(accessor.CacheControl);
            }

            return accessor;
        });

        return builder.AddCacheControl();
    }
}