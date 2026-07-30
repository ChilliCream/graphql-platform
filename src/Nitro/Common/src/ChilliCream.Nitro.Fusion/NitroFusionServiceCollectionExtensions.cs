using ChilliCream.Nitro.Fusion.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ChilliCream.Nitro.Fusion;

/// <summary>
/// Registers the Nitro Fusion deployment workflow.
/// </summary>
public static class NitroFusionServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Nitro Fusion deployment workflow and its remote transport.
    /// </summary>
    public static IServiceCollection AddNitroFusionDeploymentWorkflow(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<
            IFusionDeploymentTransportFactory,
            FusionApiTransportFactory>();
        services.TryAddSingleton<
            IFusionDeploymentWorkflow,
            FusionDeploymentWorkflow>();
        return services;
    }
}
