using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.Http;

namespace StrawberryShake
{
    /// <summary>
    /// Common extensions of <see cref="IClientBuilder"/> for <see cref="HttpConnection"/>
    /// </summary>
    public static class HttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the
        /// <see cref="IServiceCollection"/> and configures a <see cref="HttpClient"/>
        /// with the correct name
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IClientBuilder"/>
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="HttpClient"/>.
        /// </param>
        /// <param name="configureClientBuilder">
        /// A delegate that is used to additionally configure the <see cref="HttpClient"/>
        /// with a <see cref="IHttpClientBuilder"/>
        /// </param>
        public static IClientBuilder ConfigureHttpClient(
            this IClientBuilder clientBuilder,
            Action<HttpClient> configureClient,
            Action<IHttpClientBuilder>? configureClientBuilder = null)
        {
            if (clientBuilder == null)
            {
                throw new ArgumentNullException(nameof(clientBuilder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }


            IHttpClientBuilder builder = clientBuilder.Services
                .AddHttpClient(clientBuilder.ClientName, configureClient);

            configureClientBuilder?.Invoke(builder);

            return clientBuilder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the
        /// <see cref="IServiceCollection"/> and configures a <see cref="HttpClient"/>
        /// with the correct name
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IClientBuilder"/>
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="HttpClient"/>.
        /// </param>
        /// <param name="configureClientBuilder">
        /// A delegate that is used to additionally configure the <see cref="HttpClient"/>
        /// with a <see cref="IHttpClientBuilder"/>
        /// </param>
        public static IClientBuilder ConfigureHttpClient(
            this IClientBuilder clientBuilder,
            Action<IServiceProvider, HttpClient> configureClient,
            Action<IHttpClientBuilder>? configureClientBuilder = null)
        {
            if (clientBuilder == null)
            {
                throw new ArgumentNullException(nameof(clientBuilder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            IHttpClientBuilder builder = clientBuilder.Services
                .AddHttpClient(clientBuilder.ClientName, configureClient);

            configureClientBuilder?.Invoke(builder);

            return clientBuilder;
        }
    }
}
