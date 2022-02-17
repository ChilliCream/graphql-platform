using System;
using HotChocolate.Caching;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class QueryCacheRequestExecutorBuilderExtensions
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

        return builder.ConfigureSchema(b => b.AddCacheControl());
    }

    public static IRequestExecutorBuilder ModifyCacheControlOptions(this IRequestExecutorBuilder builder,
        Action<CacheControlOptions> modifyOptions)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.ConfigureSchema(s =>
        {
            if (!s.ContextData.TryGetValue(WellKnownContextData.CacheControlOptions, out var options) ||
                options is not CacheControlOptions typedOptions)
            {
                typedOptions = new CacheControlOptions();
            }

            modifyOptions(typedOptions);

            s.SetContextData(WellKnownContextData.CacheControlOptions, typedOptions);
        });

        return builder;
    }

    // todo: these should probably also not be added as global DI services?
    //       Using ConfigureSchemaServices you seem to not be able to access it as a IEnumerable<IQueryCache> though...
    public static IRequestExecutorBuilder AddQueryCache<TCache>(this IRequestExecutorBuilder builder)
        where TCache : class, IQueryCache
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IQueryCache, TCache>();

        return builder.AddCacheControl();
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

        return builder.AddCacheControl();
    }
}