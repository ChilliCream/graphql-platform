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
        public static IClientBuilder ConfigureHttpClient(
            this IClientBuilder clientBuilder,
            Action<HttpClient> configureClient)
        {
            if (clientBuilder == null)
            {
                throw new ArgumentNullException(nameof(clientBuilder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            clientBuilder.Services.AddHttpClient(clientBuilder.ClientName, configureClient);
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
        public static IClientBuilder ConfigureHttpClient(
            this IClientBuilder clientBuilder,
            Action<IServiceProvider, HttpClient> configureClient)
        {
            if (clientBuilder == null)
            {
                throw new ArgumentNullException(nameof(clientBuilder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            clientBuilder.Services.AddHttpClient(clientBuilder.ClientName, configureClient);
            return clientBuilder;
        }
    }
}
