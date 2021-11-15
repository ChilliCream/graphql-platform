using System;
using HotChocolate.Caching;
using HotChocolate.Caching.Http;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddHttpQueryCache(
        this IRequestExecutorBuilder builder,
        Action<QueryCacheSettings>? configureSettings = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddQueryCache<HttpQueryCache>(configureSettings);
    }
}