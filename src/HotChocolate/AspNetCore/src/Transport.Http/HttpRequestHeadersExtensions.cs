using System.Net.Http.Headers;

#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

/// <summary>
/// Provides extension methods for <see cref="HttpRequestHeaders"/>.
/// </summary>
public static class HttpRequestHeadersExtensions
{
    /// <summary>
    /// Adds the <c>GraphQL-Preflight</c> header to the request.
    /// </summary>
    /// <param name="headers">
    /// The <see cref="HttpRequestHeaders"/> to add the header to.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="headers"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="headers"/> is <see langword="null"/>.
    /// </exception>
    public static HttpRequestHeaders AddGraphQLPreflight(this HttpRequestHeaders headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        headers.Add("GraphQL-Preflight", "1");
        return headers;
    }
}
