using HotChocolate.Execution;
using HotChocolate.Fusion.Caching;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Fusion.Compatibility.BaselineConsumer;

/// <summary>
/// Compiled once against the pinned 16.6.4 Fusion packages (see ../artifacts/PROVENANCE.md).
/// The compatibility test harness loads this assembly unchanged, through a custom
/// AssemblyLoadContext that resolves its HotChocolate.* references against the locally built
/// 16.7 assemblies instead of the 16.6.4 copies it was compiled against, and invokes
/// <see cref="RunAsync"/> by reflection. Every line item below is unchanged 16.6 gateway-named
/// consumer source; nothing here references a router-named API.
/// </summary>
public static class BaselineEntryPoint
{
    public static async Task<string> RunAsync()
    {
        var results = new List<string>();

        // IServiceCollection.AddGraphQLGateway(): default arguments.
        var servicesDefault = new ServiceCollection();
        var builderDefault = servicesDefault.AddGraphQLGateway();
        results.Add($"AddGraphQLGateway.DefaultName={builderDefault.Name}");
        results.Add(
            $"AddGraphQLGateway.DefaultServicesIdentity={ReferenceEquals(builderDefault.Services, servicesDefault)}");

        // IServiceCollection.AddGraphQLGateway(): named, non-default argument.
        var servicesNamed = new ServiceCollection();
        var builderNamed = servicesNamed.AddGraphQLGateway(name: "baseline-named");
        results.Add($"AddGraphQLGateway.NamedName={builderNamed.Name}");

        // IServiceCollection.AddGraphQLGatewayServer(): named, non-default, out-of-declaration-order arguments.
        var servicesServer = new ServiceCollection();
        var builderServer = servicesServer.AddGraphQLGatewayServer(
            disableDefaultSecurity: true,
            name: "baseline-server",
            maxAllowedRequestSize: 654_321);
        results.Add($"AddGraphQLGatewayServer.Name={builderServer.Name}");

        // Old extension declaring-class static call (declaring-class syntax, not extension-method syntax).
        var afterCacheControl = FusionCachingGatewayBuilderExtensions.AddCacheControl(builderServer);
        results.Add(
            $"AddCacheControl.OriginalReceiverIdentity={ReferenceEquals(afterCacheControl, builderServer)}");

        var afterQueryCache = FusionCachingGatewayBuilderExtensions.UseQueryCache(afterCacheControl, after: null);
        results.Add($"UseQueryCache.OriginalReceiverIdentity={ReferenceEquals(afterQueryCache, builderServer)}");

        // A custom IFusionGatewayBuilder-only implementation: no dependency on the shipped
        // DefaultFusionGatewayBuilder / DefaultFusionRouterBuilder types.
        var customServices = new ServiceCollection();
        var customBuilder = new CustomLegacyBuilder("baseline-custom", customServices);
        var afterCustomCacheControl = FusionCachingGatewayBuilderExtensions.AddCacheControl(customBuilder);
        results.Add(
            $"CustomBuilder.OriginalReceiverIdentity={ReferenceEquals(afterCustomCacheControl, customBuilder)}");

        // A third-party-shaped extension method accepting and returning the old interface.
        var afterThirdParty = ThirdPartyGatewayExtensions.Configure(customBuilder);
        results.Add(
            $"ThirdPartyExtension.OriginalReceiverIdentity={ReferenceEquals(afterThirdParty, customBuilder)}");

        // IHostApplicationBuilder.AddGraphQLGateway(): named, non-default arguments.
        var hostBuilder = Host.CreateApplicationBuilder();
        var hostGatewayBuilder = hostBuilder.AddGraphQLGateway(
            name: "baseline-host",
            maxAllowedRequestSize: 111_222,
            disableDefaultSecurity: false);
        results.Add($"Host.AddGraphQLGateway.Name={hostGatewayBuilder.Name}");
        results.Add(
            $"Host.AddGraphQLGateway.ServicesIdentity={ReferenceEquals(hostGatewayBuilder.Services, hostBuilder.Services)}");

        // A concrete DI/configuration result reached only through the public
        // IRequestExecutorProvider path (no internal/friend API is used anywhere here): the
        // schema registered by the legacy AddGraphQLGatewayServer() builder above is
        // discoverable by name through the provider it registered.
        await using var provider = servicesServer.BuildServiceProvider();
        var executorProvider = provider.GetRequiredService<IRequestExecutorProvider>();
        results.Add($"IRequestExecutorProvider.SchemaNames={string.Join(",", executorProvider.SchemaNames.Order())}");

        return string.Join("|", results);
    }
}

file sealed class CustomLegacyBuilder(string name, IServiceCollection services) : IFusionGatewayBuilder
{
    public string Name { get; } = name;

    public IServiceCollection Services { get; } = services;
}

file static class ThirdPartyGatewayExtensions
{
    public static IFusionGatewayBuilder Configure(this IFusionGatewayBuilder builder) => builder;
}
