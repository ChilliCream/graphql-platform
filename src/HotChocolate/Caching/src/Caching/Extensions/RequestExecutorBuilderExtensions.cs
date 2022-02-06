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

    public static IRequestExecutorBuilder ModifyQueryCacheOptions(this IRequestExecutorBuilder builder,
        Action<QueryCacheSettings> modifyOptions)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton(modifyOptions);

        return builder;
    }

    private static IRequestExecutorBuilder AddQueryCacheInternals(this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IQueryCacheOptionsAccessor>(sp =>
        {
            var accessor = new QueryCacheOptionsAccessor();

            foreach (Action<QueryCacheSettings> configure in sp.GetServices<Action<QueryCacheSettings>>())
            {
                configure(accessor.QueryCache);
            }

            return accessor;
        });

        return builder
            .AddDirectiveType<CacheControlDirectiveType>()
            .AddType<CacheControlScopeType>();
    }
}