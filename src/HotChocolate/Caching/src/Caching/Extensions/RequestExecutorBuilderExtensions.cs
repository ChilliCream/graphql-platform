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

    public static IRequestExecutorBuilder AddQueryCache<TCache>(this IRequestExecutorBuilder builder,
        Action<QueryCacheSettings>? configureSettings = null)
        where TCache : class, IQueryCache
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var settings = new QueryCacheSettings();

        configureSettings?.Invoke(settings);

        builder.SetContextData(typeof(QueryCacheSettings).FullName, settings);

        builder
            .AddDirectiveType<CacheControlDirectiveType>()
            .AddType<CacheControlScopeType>();

        builder.Services.AddSingleton<IQueryCache, TCache>();

        return builder;
    }
}