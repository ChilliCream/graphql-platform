using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

internal sealed class DefaultFusionRouterBuilder : IFusionRouterBuilder
{
    public DefaultFusionRouterBuilder(IServiceCollection services, string name)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(name);

        Services = services;
        Name = name;
    }

    public string Name { get; }

    public IServiceCollection Services { get; }
}
