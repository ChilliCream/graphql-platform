using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// Extensions methods to configure an <see cref="IServiceCollection"/> for
    /// <see cref="ISocketClientPool"/>.
    /// </summary>
    public static class WebSocketClientPoolServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the <see cref="ISocketClientPool"/> on the <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/>
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Throws when <paramref name="services"/> is <c>null</c>
        /// </exception>
        public static IServiceCollection AddWebSocketClientPool(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<ISocketClientPool, SocketClientPool>();
            return services;
        }
    }
}
