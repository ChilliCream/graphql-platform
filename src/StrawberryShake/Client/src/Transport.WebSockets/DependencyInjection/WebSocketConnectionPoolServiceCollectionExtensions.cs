using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrawberryShake.Transport.WebSockets
{
    public static class WebSocketClientPoolServiceCollectionExtensions
    {
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
