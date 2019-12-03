using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StrawberryShake.Configuration
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IOperationClientBuilder"/>
    /// </summary>
    public static class OperationClientBuilderExtensions
    {
        /// <summary>
        /// Configures the client options that will be used to create a operation client.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configure">
        /// A delegate that is used to configure the <see cref="ClientOptions"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IOperationClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IOperationClientBuilder ConfigureClient(
            this IOperationClientBuilder builder,
            Action<ClientOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.Configure<ClientOptions>(
                builder.Name,
                options => configure(options));

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="WebSocketClient"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="WebSocketClient"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IWebSocketClientBuilder"/> that can be used to configure the client.
        /// </returns>
        /// <remarks>
        /// The <see cref="IServiceProvider"/> provided to <paramref name="configureClient"/>
        /// will be the application's root service provider instance.
        /// </remarks>
        public static IOperationClientBuilder ConfigureClient(
            this IOperationClientBuilder builder,
            Action<IServiceProvider, ClientOptions> configureClient)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            builder.Services.AddTransient<IConfigureOptions<ClientOptions>>(sp =>
                new ConfigureNamedOptions<ClientOptions>(
                    builder.Name,
                    options => configureClient(sp, options)));

            return builder;
        }
    }
}
