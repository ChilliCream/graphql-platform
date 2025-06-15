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

        services.AddOptions();

        services.TryAddSingleton<FusionRequestExecutorManager>();
        services.TryAddSingleton<IRequestExecutorProvider>(
            sp => sp.GetRequiredService<FusionRequestExecutorManager>());
        services.TryAddSingleton<IRequestExecutorEvents>(
            sp => sp.GetRequiredService<FusionRequestExecutorManager>());

        var builder = new DefaultFusionGatewayBuilder(services, name);

        builder.UseDefaultPipeline();

        return builder;
    }
}
