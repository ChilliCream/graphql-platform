using System;
using StrawberryShake.Transport.WebSockets;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
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
