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

    internal static void AddResultObjectPools(
        IServiceCollection services,
        FusionMemoryPoolOptions options)
    {
        services.TryAddSingleton<ObjectPoolProvider>(
            static _ => new DefaultObjectPoolProvider());

        services.TryAddSingleton(options);

        services.TryAddSingleton(static provider =>
        {
            var options = provider.GetRequiredService<FusionMemoryPoolOptions>();
            var providerPool = provider.GetRequiredService<ObjectPoolProvider>();
            return providerPool.Create(new ResultDataBatchPoolPolicy<ObjectResult>(
                options.ObjectBatchSize,
                options.DefaultObjectCapacity,
                options.MaxAllowedObjectCapacity));
        });

        services.TryAddSingleton(static provider =>
        {
            var options = provider.GetRequiredService<FusionMemoryPoolOptions>();
            var providerPool = provider.GetRequiredService<ObjectPoolProvider>();
            return providerPool.Create(new ResultDataBatchPoolPolicy<LeafFieldResult>(options.LeafFieldBatchSize));
        });

        services.TryAddSingleton(static provider =>
        {
            var options = provider.GetRequiredService<FusionMemoryPoolOptions>();
            var providerPool = provider.GetRequiredService<ObjectPoolProvider>();
            return providerPool.Create(new ResultDataBatchPoolPolicy<ListFieldResult>(options.ListFieldBatchSize));
        });

        services.TryAddSingleton(static provider =>
        {
            var options = provider.GetRequiredService<FusionMemoryPoolOptions>();
            var providerPool = provider.GetRequiredService<ObjectPoolProvider>();
            return providerPool.Create(new ResultDataBatchPoolPolicy<ObjectFieldResult>(options.ObjectFieldBatchSize));
        });

        services.TryAddSingleton(static provider =>
        {
            var options = provider.GetRequiredService<FusionMemoryPoolOptions>();
            var providerPool = provider.GetRequiredService<ObjectPoolProvider>();
            return providerPool.Create(new ResultDataBatchPoolPolicy<ObjectListResult>(
                options.ListBatchSize,
                options.DefaultListCapacity,
                options.MaxAllowedListCapacity));
        });

        services.TryAddSingleton(static provider =>
        {
            var options = provider.GetRequiredService<FusionMemoryPoolOptions>();
            var providerPool = provider.GetRequiredService<ObjectPoolProvider>();
            return providerPool.Create(new ResultDataBatchPoolPolicy<NestedListResult>(
                options.ListBatchSize,
                options.DefaultListCapacity,
                options.MaxAllowedListCapacity));
        });

        services.TryAddSingleton(static provider =>
        {
            var options = provider.GetRequiredService<FusionMemoryPoolOptions>();
            var providerPool = provider.GetRequiredService<ObjectPoolProvider>();
            return providerPool.Create(new ResultDataBatchPoolPolicy<LeafListResult>(
                options.ListBatchSize,
                options.DefaultListCapacity,
                options.MaxAllowedListCapacity));
        });

        services.TryAddSingleton(static provider =>
        {
            var providerPool = provider.GetRequiredService<ObjectPoolProvider>();
            return providerPool.Create(new ResultPoolSessionPolicy(
                provider.GetRequiredService<ObjectPool<ResultDataBatch<ObjectResult>>>(),
                provider.GetRequiredService<ObjectPool<ResultDataBatch<LeafFieldResult>>>(),
                provider.GetRequiredService<ObjectPool<ResultDataBatch<ListFieldResult>>>(),
                provider.GetRequiredService<ObjectPool<ResultDataBatch<ObjectFieldResult>>>(),
                provider.GetRequiredService<ObjectPool<ResultDataBatch<ObjectListResult>>>(),
                provider.GetRequiredService<ObjectPool<ResultDataBatch<NestedListResult>>>(),
                provider.GetRequiredService<ObjectPool<ResultDataBatch<LeafListResult>>>()));
        });

        services.TryAddScoped(static provider =>
            new ResultPoolSessionHolder(
                provider.GetRequiredService<ObjectPool<ResultPoolSession>>()));

        services.TryAddScoped(static provider =>
            provider.GetRequiredService<ResultPoolSessionHolder>().Session);
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
