using System;
using Microsoft.Extensions.DependencyInjection;

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

            return services.AddSingleton<ISocketConnectionPool, WebSocketConnectionPool>();
        }
    }
}
