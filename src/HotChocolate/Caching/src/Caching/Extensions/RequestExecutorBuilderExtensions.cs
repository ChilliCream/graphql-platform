using System;
using HotChocolate.Caching;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder UseQueryResultCache(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest<QueryCacheMiddleware>();

    public static IRequestExecutorBuilder UseQueryResultCachePipeline(this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder
            .UseInstrumentations()
            .UseExceptions()
            .UseTimeout()
            .UseQueryResultCache()
            .UseDocumentCache()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationCache()
            .UseOperationComplexityAnalyzer()
            .UseOperationResolver()
            .UseOperationVariableCoercion()
            .UseOperationExecution();
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

        return builder
            .AddDirectiveType<CacheControlDirectiveType>()
            .AddType<CacheControlScopeType>();
    }
}