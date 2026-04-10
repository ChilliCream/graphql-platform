#if FUSION
using HotChocolate.Fusion.Transport.Http;
#else
using HotChocolate.Transport.Http;
#endif

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring request deduplication on an <see cref="IHttpClientBuilder"/>.
/// </summary>
public static class RequestDeduplicationHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="RequestDeduplicationHandler"/> to the HTTP client pipeline
    /// that deduplicates identical in-flight requests. Only GraphQL query operations
    /// (signaled via the <c>GraphQL-Operation-Type</c> header) are deduplicated.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="configure">
    /// An optional action to configure <see cref="RequestDeduplicationOptions"/>,
    /// such as specifying which headers participate in the deduplication hash.
    /// </param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for chaining.</returns>
    public static IHttpClientBuilder AddRequestDeduplication(
        this IHttpClientBuilder builder,
        Action<RequestDeduplicationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new RequestDeduplicationOptions();
        configure?.Invoke(options);

        return builder.AddHttpMessageHandler(() => new RequestDeduplicationHandler(options));
    }
}
