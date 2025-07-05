using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
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

        AddRequestExecutorManager(services);
        AddSourceSchemaScope(services);

        return CreateBuilder(services, name);
    }

    private static void AddRequestExecutorManager(
        IServiceCollection services)
    {
        services.TryAddSingleton<FusionRequestExecutorManager>();
        services.TryAddSingleton<IRequestExecutorProvider>(
            sp => sp.GetRequiredService<FusionRequestExecutorManager>());
        services.TryAddSingleton<IRequestExecutorEvents>(
            sp => sp.GetRequiredService<FusionRequestExecutorManager>());
    }

    private static void AddSourceSchemaScope(
        IServiceCollection services)
    {
        services.TryAddSingleton<ISourceSchemaClientScopeFactory>(
            static sp => new DefaultSourceSchemaClientScopeFactory(
                sp.GetRequiredService<IHttpClientFactory>()));
    }

    private static DefaultFusionGatewayBuilder CreateBuilder(
        IServiceCollection services,
        string name)
    {
        var builder = new DefaultFusionGatewayBuilder(services, name);
        builder.UseDefaultPipeline();
        return builder;
    }
}
