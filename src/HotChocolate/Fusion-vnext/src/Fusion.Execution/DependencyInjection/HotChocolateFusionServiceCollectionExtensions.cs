using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class HotChocolateFusionServiceCollectionExtensions
{
    public static IFusionGatewayBuilder AddGraphQLGateway(
        this IServiceCollection services,
        string? name = null,
        FusionMemoryPoolOptions? options = null)
    {
        name ??= ISchemaDefinition.DefaultName;
        options ??= new FusionMemoryPoolOptions();

        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(name);

        services.AddOptions();

        AddRequestExecutorManager(services);
        AddSourceSchemaScope(services);
        AddResultObjectPools(services, options.Clone());

        return CreateBuilder(services, name);
    }

    private static void AddRequestExecutorManager(
        IServiceCollection services)
    {
        services.TryAddSingleton(
            static sp => new FusionRequestExecutorManager(
                sp.GetRequiredService<IOptionsMonitor<FusionGatewaySetup>>(),
                sp));
        services.TryAddSingleton<IRequestExecutorProvider>(
            static sp => sp.GetRequiredService<FusionRequestExecutorManager>());
        services.TryAddSingleton<IRequestExecutorEvents>(
            static sp => sp.GetRequiredService<FusionRequestExecutorManager>());
    }

    private static void AddSourceSchemaScope(
        IServiceCollection services)
    {
        services.TryAddSingleton<ISourceSchemaClientScopeFactory>(
            static sp => new DefaultSourceSchemaClientScopeFactory(
                sp.GetRequiredService<IHttpClientFactory>()));
    }

    // TODO : REVIEW IF THIS IS STILL NEEDED
    internal static void AddResultObjectPools(
        IServiceCollection services,
        FusionMemoryPoolOptions options)
    {
        services.TryAddSingleton<ObjectPoolProvider>(static _ => new DefaultObjectPoolProvider());
        services.TryAddSingleton(options);
    }

    private static DefaultFusionGatewayBuilder CreateBuilder(
        IServiceCollection services,
        string name)
    {
        if (!services.Any(x =>
            x.ServiceType == typeof(SchemaName)
                && x.ImplementationInstance is SchemaName s
                && s.Value.Equals(name, StringComparison.Ordinal)))
        {
            services.AddSingleton(new SchemaName(name));
        }

        var builder = new DefaultFusionGatewayBuilder(services, name);
        builder.UseDefaultPipeline();
        return builder;
    }
}
