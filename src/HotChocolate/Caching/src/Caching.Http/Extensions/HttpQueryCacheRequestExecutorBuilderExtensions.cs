using System;
using HotChocolate.Caching.Http;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class HttpQueryCacheRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="HttpQueryCache"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    public static IRequestExecutorBuilder AddHttpQueryCache(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddQueryCache<HttpQueryCache>();
    }
}
