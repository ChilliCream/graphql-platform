using System.Net.Http.Headers;
using StrawberryShake;
using StrawberryShake.Transport.Http;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Common extensions of <see cref="IClientBuilder{T}"/> for <see cref="HttpConnection"/>
/// </summary>
public static class HttpClientBuilderExtensions
{
    private const string UserAgentName = "StrawberryShake";
    private static readonly string s_userAgentVersion =
        typeof(HttpClientBuilderExtensions).Assembly.GetName().Version!.ToString();

    /// <summary>
    /// Adds the <see cref="IHttpClientFactory"/> and related services to the
    /// <see cref="IServiceCollection"/> and configures a <see cref="HttpClient"/>
    /// with the correct name
    /// </summary>
    /// <param name="clientBuilder">
    /// The <see cref="IClientBuilder{T}"/>
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="HttpClient"/>.
    /// </param>
    /// <param name="configureClientBuilder">
    /// A delegate that is used to additionally configure the <see cref="HttpClient"/>
    /// with a <see cref="IHttpClientBuilder"/>
    /// </param>
    public static IClientBuilder<T> ConfigureHttpClient<T>(
        this IClientBuilder<T> clientBuilder,
        Action<HttpClient> configureClient,
        Action<IHttpClientBuilder>? configureClientBuilder = null)
        where T : IStoreAccessor
    {
        ArgumentNullException.ThrowIfNull(clientBuilder);
        ArgumentNullException.ThrowIfNull(configureClient);

        var builder = clientBuilder.Services
            .AddHttpClient(
                clientBuilder.ClientName,
                client =>
                {
                    client.DefaultRequestHeaders.UserAgent.Add(
                        new ProductInfoHeaderValue(
                            new ProductHeaderValue(
                                UserAgentName,
                                s_userAgentVersion)));
                    configureClient(client);
                });

        configureClientBuilder?.Invoke(builder);

        return clientBuilder;
    }

    /// <summary>
    /// Adds the <see cref="IHttpClientFactory"/> and related services to the
    /// <see cref="IServiceCollection"/> and configures a <see cref="HttpClient"/>
    /// with the correct name
    /// </summary>
    /// <param name="clientBuilder">
    /// The <see cref="IClientBuilder{T}"/>
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="HttpClient"/>.
    /// </param>
    /// <param name="configureClientBuilder">
    /// A delegate that is used to additionally configure the <see cref="HttpClient"/>
    /// with a <see cref="IHttpClientBuilder"/>
    /// </param>
    public static IClientBuilder<T> ConfigureHttpClient<T>(
        this IClientBuilder<T> clientBuilder,
        Action<IServiceProvider, HttpClient> configureClient,
        Action<IHttpClientBuilder>? configureClientBuilder = null)
        where T : IStoreAccessor
    {
        ArgumentNullException.ThrowIfNull(clientBuilder);
        ArgumentNullException.ThrowIfNull(configureClient);

        var builder = clientBuilder.Services
            .AddHttpClient(clientBuilder.ClientName, (sp, client) =>
            {
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue(
                        new ProductHeaderValue(
                            UserAgentName,
                            s_userAgentVersion)));
                configureClient(sp, client);
            });

        configureClientBuilder?.Invoke(builder);

        return clientBuilder;
    }
}
