using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Results;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

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

        AddCore(services);
        AddRequestExecutorManager(services);
        AddSourceSchemaScope(services);

        return CreateBuilder(services, name);
    }

    private static void AddCore(
        IServiceCollection services)
    {
        services.AddOptions();

        services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        services.TryAddSingleton(sp =>
        {
            var provider = sp.GetRequiredService<ObjectPoolProvider>();
            var policy = new StringBuilderPooledObjectPolicy();
            return provider.Create(policy);
        });
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
        builder.AddDocumentCache();
        builder.AddFetchResultStorePool();
        builder.UseDefaultPipeline();
        return builder;
    }

    private static void AddFetchResultStorePool(this IFusionGatewayBuilder builder)
        => builder.ConfigureSchemaServices(
            static (_, s) => s.TryAddSingleton(
                new FetchResultStorePool(
                    levels: [64, 128, 256, 512],
                    trimInterval: TimeSpan.FromMinutes(5))));

    private static IFusionGatewayBuilder AddDocumentCache(this IFusionGatewayBuilder builder)
    {
        builder.Services.TryAddKeyedSingleton<IDocumentCache>(
            builder.Name,
            static (sp, schemaName) =>
            {
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<FusionGatewaySetup>>();
                var setup = optionsMonitor.Get((string)schemaName!);

                var options = FusionRequestExecutorManager.CreateOptions(setup);

                return new DefaultDocumentCache(options.OperationDocumentCacheSize);
            });

        return builder.ConfigureSchemaServices(
            static (applicationServices, s) =>
                s.AddSingleton(schemaServices =>
                {
                    var schemaName = schemaServices.GetRequiredService<ISchemaDefinition>().Name;
                    return applicationServices.GetRequiredKeyedService<IDocumentCache>(schemaName);
                }));
    }
}
