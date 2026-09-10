using System.Reflection;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Caching;

public sealed class CachingRouterBuilderCompatibilityTests
{
    // Reflection intentionally inspects the obsolete IFusionGatewayBuilder type.
#pragma warning disable CS0618
    [Fact]
    public void GatewayExtensions_Should_BeObsolete_When_RouterExtensionsAreAdded()
    {
        // arrange
        var legacyType = typeof(FusionCachingGatewayBuilderExtensions);
        var routerType = typeof(FusionCachingRouterBuilderExtensions);
        var legacyMethods = legacyType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        var routerMethods = routerType.GetMethods(BindingFlags.Public | BindingFlags.Static);

        // act & assert
        Assert.Equal(3, legacyMethods.Length);
        Assert.All(legacyMethods, m =>
        {
            Assert.Equal(typeof(IFusionGatewayBuilder), m.GetParameters()[0].ParameterType);
            var obsolete = Assert.Single(m.GetCustomAttributes<ObsoleteAttribute>());
            Assert.Equal($"Use {m.Name} on IFusionRouterBuilder instead.", obsolete.Message);
        });

        Assert.Equal(
            legacyMethods.Select(m => m.Name).Order(),
            routerMethods.Select(m => m.Name).Order());
        Assert.All(routerMethods, m =>
        {
            Assert.Equal(typeof(IFusionRouterBuilder), m.GetParameters()[0].ParameterType);
            Assert.Empty(m.GetCustomAttributes<ObsoleteAttribute>());
        });
    }
#pragma warning restore CS0618

    [Fact]
    public void AddCacheControl_Should_ConfigureSchemaOnce_When_UsingRouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
    {
        // arrange
        var baselineServices = new ServiceCollection();
        baselineServices.AddGraphQLRouterCore("baseline-schema");
        using var baselineProvider = baselineServices.BuildServiceProvider();
        var baselineSetup = baselineProvider
            .GetRequiredService<IOptionsMonitor<FusionRouterSetup>>()
            .Get("baseline-schema");
        var baselineSchemaServiceModifiers = baselineSetup.SchemaServiceModifiers.Count;
        var baselinePipelineModifiers = baselineSetup.PipelineModifiers.Count;

        var routerServices = new ServiceCollection();
        var router = routerServices.AddGraphQLRouterCore("router-schema");

        var legacyServices = new ServiceCollection();
#pragma warning disable CS0618 // Intentional legacy static call.
        var legacy = legacyServices.AddGraphQLGateway("legacy-schema");
#pragma warning restore CS0618

        var customServices = new ServiceCollection();
#pragma warning disable CS0618 // Models a third-party legacy-only builder.
        var customBuilder = new LegacyOnlyBuilder("custom-schema", customServices);
#pragma warning restore CS0618

        // act
        Assert.Same(router, router.AddCacheControl().UseQueryCache());
#pragma warning disable CS0618 // Intentional legacy static calls.
        Assert.Same(legacy, FusionCachingGatewayBuilderExtensions.UseQueryCache(
            FusionCachingGatewayBuilderExtensions.AddCacheControl(legacy)));
        Assert.Same(customBuilder, FusionCachingGatewayBuilderExtensions.UseQueryCache(
            FusionCachingGatewayBuilderExtensions.AddCacheControl(customBuilder)));
#pragma warning restore CS0618

        // assert
        // AddCacheControl registers a schema-service modifier for the options accessor and
        // another (via AddOperationPlannerInterceptor) for the interceptor; UseQueryCache adds
        // one pipeline modifier. Each surface should add exactly that, once, on top of what
        // AddGraphQLRouterCore itself already registers.
        using var routerProvider = routerServices.BuildServiceProvider();
        using var legacyProvider = legacyServices.BuildServiceProvider();
        using var customProvider = customServices.BuildServiceProvider();

        var routerSetup = routerProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("router-schema");
        var legacySetup = legacyProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("legacy-schema");
        var customSetup = customProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("custom-schema");

        // router and legacy both go through AddGraphQLRouterCore/AddGraphQLGateway, so they share
        // the same baseline; the custom builder never called that, so its baseline is zero.
        Assert.Equal(baselineSchemaServiceModifiers + 2, routerSetup.SchemaServiceModifiers.Count);
        Assert.Equal(baselinePipelineModifiers + 1, routerSetup.PipelineModifiers.Count);
        Assert.Equal(routerSetup.SchemaServiceModifiers.Count, legacySetup.SchemaServiceModifiers.Count);
        Assert.Equal(routerSetup.PipelineModifiers.Count, legacySetup.PipelineModifiers.Count);
        Assert.Equal(2, customSetup.SchemaServiceModifiers.Count);
        Assert.Single(customSetup.PipelineModifiers);
    }

#pragma warning disable CS0618 // This builder deliberately implements only the published legacy contract.
    private sealed class LegacyOnlyBuilder(string name, IServiceCollection services) : IFusionGatewayBuilder
    {
        public string Name { get; } = name;

        public IServiceCollection Services { get; } = services;
    }
#pragma warning restore CS0618
}
