using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Extension methods for registering object pooling services used by the message bus infrastructure.
/// </summary>
public static class PoolingMessageBusExtensions
{
    internal static IServiceCollection AddPoolingCore(this IServiceCollection services)
    {
        services.TryAddSingleton<ObjectPool<DispatchContext>, DispatchContextPool>();
        services.TryAddSingleton<ObjectPool<ReceiveContext>, ReceiveContextPool>();
        services.TryAddSingleton<IMessagingPools, MessagingPools>();

        return services;
    }
}
