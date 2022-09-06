using System;
using HotChocolate;
using HotChocolate.Caching;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class QueryCacheRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder UseQueryCache(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest<QueryCacheMiddleware>();

    public static IRequestExecutorBuilder UseQueryCachePipeline(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder
            .UseInstrumentations()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseQueryCache()
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

        builder.ConfigureSchemaServices(services =>
        {
            services.AddOptions();
            services.AddSingleton<ICacheControlOptionsAccessor, CacheControlOptionsAccessor>();
        });

        return builder.ConfigureSchema(b =>
        {
            b.AddCacheControl();
            b.TryAddTypeInterceptor<CacheControlTypeInterceptor>();
        });
    }

    public static IRequestExecutorBuilder ModifyCacheControlOptions(
        this IRequestExecutorBuilder builder,
        Action<CacheControlOptions> modifyOptions)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (modifyOptions is null)
        {
            throw new ArgumentNullException(nameof(modifyOptions));
        }

        builder.ConfigureSchemaServices(services =>
        {
            services.Configure(modifyOptions);
        });

        return builder;
    }

    internal static IRequestExecutorBuilder AddQueryCache<TCache>(
        this IRequestExecutorBuilder builder)
        where TCache : class, IQueryCache
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.ConfigureSchemaServices(services =>
        {
            services.AddSingleton<IQueryCache, TCache>();
        });

        return builder.AddCacheControl();
    }

    internal static IRequestExecutorBuilder AddQueryCache<TCache>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, TCache> cacheFactory)
        where TCache : class, IQueryCache
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.ConfigureSchemaServices(services =>
        {
            services.AddSingleton<IQueryCache>(cacheFactory);
        });

        return builder.AddCacheControl();
    }
}
