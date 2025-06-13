using HotChocolate;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class HotChocolateFusionServiceCollectionExtensions
{
    public static IFusionGatewayBuilder AddGraphQLGateway(
        this IServiceCollection services,
        string name = "__Default")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(name);

        services.Configure<FusionGatewaySetup>((o) =>
        {
            o.SchemaServiceModifiers.Add(
                static (rs, ss) => ss.TryAddSingleton<IRootServiceProviderAccessor>(
                    new RootServiceProviderAccessor(rs)));
        });

        return new DefaultFusionGatewayBuilder(services, name);
    }
}
