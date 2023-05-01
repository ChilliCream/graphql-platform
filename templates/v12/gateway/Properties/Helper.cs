using Demo.Gateway.Helpers;
using HotChocolate.Fusion.Clients;

namespace Microsoft.Extensions.DependencyInjection;

public static class Helper
{
    public static IServiceCollection AddWebSocketClient(this IServiceCollection services)
    {
        services.AddSingleton<IWebSocketConnectionFactory, WebSocketConnectionFactory>();
        return services;
    }
}