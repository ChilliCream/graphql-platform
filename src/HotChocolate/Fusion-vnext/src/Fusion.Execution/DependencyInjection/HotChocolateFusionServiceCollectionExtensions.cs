using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class HotChocolateFusionServiceCollectionExtensions
{
    public static IFusionGatewayBuilder AddGraphQLGateway(
        this IServiceCollection services,
        string? name = null)
    {
        name ??= ISchemaDefinition.DefaultName;

        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(name);

        services.Configure<FusionGatewaySetup>(
            ISchemaDefinition.DefaultName,
            (o) =>
            {
                o.SchemaServiceModifiers.Add(
                    static (rs, ss) => ss.TryAddSingleton<IRootServiceProviderAccessor>(
                        new RootServiceProviderAccessor(rs)));
            });

        services.TryAddSingleton<FusionRequestExecutorManager>();
        services.TryAddSingleton<IRequestExecutorProvider>(
            sp => sp.GetRequiredService<FusionRequestExecutorManager>());
        services.TryAddSingleton<IRequestExecutorEvents>(
            sp => sp.GetRequiredService<FusionRequestExecutorManager>());

        return new DefaultFusionGatewayBuilder(services, name);
    }
}
