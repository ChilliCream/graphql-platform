using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrawberryShake.Transport.WebSockets
{
    public static class WebSocketConnectionPoolServiceCollectionExtensions
    {
        public static IServiceCollection AddWebSocketConnectionPool(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<ISocketConnectionPool, WebSocketConnectionPool>();
            return services;
        }
    }
}
