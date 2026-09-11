using System.Reflection;
using HotChocolate.Adapters.Mcp.Configuration;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Extensions;

public sealed class FusionRouterBuilderCompatibilityTests
{
    // Reflection intentionally inspects the obsolete IFusionGatewayBuilder type.
#pragma warning disable CS0618
    [Fact]
    public void GatewayExtensions_Should_BeObsolete_When_RouterExtensionsAreAdded()
    {
        // arrange
        var legacyType = typeof(FusionGatewayBuilderExtensions);
        var routerType = typeof(FusionRouterBuilderExtensions);
        var legacyMethods = legacyType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        var routerMethods = routerType.GetMethods(BindingFlags.Public | BindingFlags.Static);

        // act & assert
        Assert.Equal(5, legacyMethods.Length);
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
    public void AddMcp_Should_ConfigureSchemaServicesOnce_When_UsingRouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
    {
        // arrange
        var baselineServices = new ServiceCollection();
        baselineServices.AddGraphQLRouterCore("baseline-schema");
        using var baselineProvider = baselineServices.BuildServiceProvider();
        var baselineCount = baselineProvider
            .GetRequiredService<IOptionsMonitor<FusionRouterSetup>>()
            .Get("baseline-schema")
            .SchemaServiceModifiers.Count;

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
        Assert.Same(router, router.AddMcp());
#pragma warning disable CS0618 // Intentional legacy static calls.
        Assert.Same(legacy, FusionGatewayBuilderExtensions.AddMcp(legacy));
        Assert.Same(customBuilder, FusionGatewayBuilderExtensions.AddMcp(customBuilder));
#pragma warning restore CS0618

        // assert
        // AddMcp registers one schema-service modifier for the MCP schema services and, through
        // AddWarmupTask, another for the storage warmup task; each surface should add exactly
        // that, once, on top of what AddGraphQLRouterCore/AddGraphQLGateway already registers.
        using var routerProvider = routerServices.BuildServiceProvider();
        using var legacyProvider = legacyServices.BuildServiceProvider();
        using var customProvider = customServices.BuildServiceProvider();

        var routerCount = routerProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("router-schema").SchemaServiceModifiers.Count;
        var legacyCount = legacyProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("legacy-schema").SchemaServiceModifiers.Count;
        var customCount = customProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("custom-schema").SchemaServiceModifiers.Count;

        Assert.Equal(baselineCount + 2, routerCount);
        Assert.Equal(routerCount, legacyCount);
        Assert.Equal(2, customCount);
    }

    [Fact]
    public void AddMcp_Should_RegisterNamedServerCallbacksOnce_When_UsingRouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
    {
        // arrange
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

        var optionCalls = new List<string>();
        var serverCalls = new List<string>();

        // act
        router.AddMcp(_ => optionCalls.Add("router"), _ => serverCalls.Add("router"));
#pragma warning disable CS0618 // Intentional legacy static calls.
        FusionGatewayBuilderExtensions.AddMcp(legacy, _ => optionCalls.Add("legacy"), _ => serverCalls.Add("legacy"));
        FusionGatewayBuilderExtensions.AddMcp(customBuilder, _ => optionCalls.Add("custom"), _ => serverCalls.Add("custom"));
#pragma warning restore CS0618

        // assert
        using var routerProvider = routerServices.BuildServiceProvider();
        using var legacyProvider = legacyServices.BuildServiceProvider();
        using var customProvider = customServices.BuildServiceProvider();

        var routerSetup = routerProvider.GetRequiredService<IOptionsMonitor<McpSetup>>().Get("router-schema");
        var legacySetup = legacyProvider.GetRequiredService<IOptionsMonitor<McpSetup>>().Get("legacy-schema");
        var customSetup = customProvider.GetRequiredService<IOptionsMonitor<McpSetup>>().Get("custom-schema");

        // Each surface should register exactly one server-options modifier and one server
        // modifier; invoking them proves the original delegate is the one that was wired.
        foreach (var setup in new[] { routerSetup, legacySetup, customSetup })
        {
            Assert.Single(setup.ServerOptionsModifiers).Invoke(new McpServerOptions());
            Assert.Single(setup.ServerModifiers).Invoke(null!);
        }

        Assert.Equal(["router", "legacy", "custom"], optionCalls);
        Assert.Equal(["router", "legacy", "custom"], serverCalls);
    }

    [Fact]
    public void AddMcp_Should_RegisterWarmupTask_When_SkipIfFalse_And_Omit_When_SkipIfTrue()
    {
        // arrange
        var runServices = new ServiceCollection();
        var runBuilder = runServices.AddGraphQLRouterCore("run-schema");
        runBuilder.AddMcp(skipIf: _ => false);

        var skipServices = new ServiceCollection();
        var skipBuilder = skipServices.AddGraphQLRouterCore("skip-schema");
        skipBuilder.AddMcp(skipIf: _ => true);

        using var runProvider = runServices.BuildServiceProvider();
        using var skipProvider = skipServices.BuildServiceProvider();
        var runSetup = runProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("run-schema");
        var skipSetup = skipProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("skip-schema");

        var runSchemaServices = new ServiceCollection();
        var skipSchemaServices = new ServiceCollection();

        // act
        // AddMcp registers the warmup modifier last, after the schema services one.
        runSetup.SchemaServiceModifiers[^1](runProvider, runSchemaServices);
        skipSetup.SchemaServiceModifiers[^1](skipProvider, skipSchemaServices);

        // assert
        // The modifier registers into a fresh, otherwise-empty collection, so the descriptor
        // count fully describes whether the warmup task registration was skipped.
        Assert.Equal(
            [typeof(IRequestExecutorWarmupTask)],
            runSchemaServices.Select(d => d.ServiceType));
        Assert.Empty(skipSchemaServices);
    }

    [Fact]
    public void ModifyMcpToolOptions_Should_ConfigureSchemaServicesOnce_When_UsingRouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
    {
        // arrange
        var baselineServices = new ServiceCollection();
        baselineServices.AddGraphQLRouterCore("baseline-schema");
        using var baselineProvider = baselineServices.BuildServiceProvider();
        var baselineCount = baselineProvider
            .GetRequiredService<IOptionsMonitor<FusionRouterSetup>>()
            .Get("baseline-schema")
            .SchemaServiceModifiers.Count;

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
        Assert.Same(router, router.ModifyMcpToolOptions(o => o.UseJsonSchemaReferences = false));
#pragma warning disable CS0618 // Intentional legacy static calls.
        Assert.Same(legacy, FusionGatewayBuilderExtensions.ModifyMcpToolOptions(legacy, o => o.UseJsonSchemaReferences = false));
        Assert.Same(customBuilder, FusionGatewayBuilderExtensions.ModifyMcpToolOptions(customBuilder, o => o.UseJsonSchemaReferences = false));
#pragma warning restore CS0618

        // assert
        using var routerProvider = routerServices.BuildServiceProvider();
        using var legacyProvider = legacyServices.BuildServiceProvider();
        using var customProvider = customServices.BuildServiceProvider();

        var routerCount = routerProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("router-schema").SchemaServiceModifiers.Count;
        var legacyCount = legacyProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("legacy-schema").SchemaServiceModifiers.Count;
        var customCount = customProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("custom-schema").SchemaServiceModifiers.Count;

        Assert.Equal(baselineCount + 1, routerCount);
        Assert.Equal(routerCount, legacyCount);
        Assert.Equal(1, customCount);
    }

    [Fact]
    public void AddMcpStorage_Should_SetStorageFactoryOnce_When_UsingRouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
    {
        // arrange
        var storage = new TestMcpStorage();

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
        Assert.Same(router, router.AddMcpStorage(storage));
#pragma warning disable CS0618 // Intentional legacy static calls.
        Assert.Same(legacy, FusionGatewayBuilderExtensions.AddMcpStorage(legacy, storage));
        Assert.Same(customBuilder, FusionGatewayBuilderExtensions.AddMcpStorage(customBuilder, storage));
#pragma warning restore CS0618

        // assert
        using var routerProvider = routerServices.BuildServiceProvider();
        using var legacyProvider = legacyServices.BuildServiceProvider();
        using var customProvider = customServices.BuildServiceProvider();

        Assert.Same(storage, ResolveStorage(routerProvider, "router-schema"));
        Assert.Same(storage, ResolveStorage(legacyProvider, "legacy-schema"));
        Assert.Same(storage, ResolveStorage(customProvider, "custom-schema"));
    }

    [Fact]
    public void AddMcpStorage_Should_SetStorageFactoryOnce_When_UsingGenericOverload_Across_RouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
    {
        // arrange
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
        Assert.Same(router, router.AddMcpStorage<TestMcpStorage>());
#pragma warning disable CS0618 // Intentional legacy static calls.
        Assert.Same(legacy, FusionGatewayBuilderExtensions.AddMcpStorage<TestMcpStorage>(legacy));
        Assert.Same(customBuilder, FusionGatewayBuilderExtensions.AddMcpStorage<TestMcpStorage>(customBuilder));
#pragma warning restore CS0618

        // assert
        using var routerProvider = routerServices.BuildServiceProvider();
        using var legacyProvider = legacyServices.BuildServiceProvider();
        using var customProvider = customServices.BuildServiceProvider();

        Assert.IsType<TestMcpStorage>(ResolveStorage(routerProvider, "router-schema"));
        Assert.IsType<TestMcpStorage>(ResolveStorage(legacyProvider, "legacy-schema"));
        Assert.IsType<TestMcpStorage>(ResolveStorage(customProvider, "custom-schema"));
    }

    [Fact]
    public void AddMcpStorage_Should_SetStorageFactoryOnce_When_UsingFactoryOverload_Across_RouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
    {
        // arrange
        var storage = new TestMcpStorage();
        Func<IServiceProvider, IMcpStorage> factory = _ => storage;

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
        Assert.Same(router, router.AddMcpStorage(factory));
#pragma warning disable CS0618 // Intentional legacy static calls.
        Assert.Same(legacy, FusionGatewayBuilderExtensions.AddMcpStorage(legacy, factory));
        Assert.Same(customBuilder, FusionGatewayBuilderExtensions.AddMcpStorage(customBuilder, factory));
#pragma warning restore CS0618

        // assert
        using var routerProvider = routerServices.BuildServiceProvider();
        using var legacyProvider = legacyServices.BuildServiceProvider();
        using var customProvider = customServices.BuildServiceProvider();

        Assert.Same(storage, ResolveStorage(routerProvider, "router-schema"));
        Assert.Same(storage, ResolveStorage(legacyProvider, "legacy-schema"));
        Assert.Same(storage, ResolveStorage(customProvider, "custom-schema"));
    }

    private static IMcpStorage? ResolveStorage(ServiceProvider provider, string schemaName)
    {
        var setup = provider.GetRequiredService<IOptionsMonitor<McpSetup>>().Get(schemaName);
        return setup.StorageFactory?.Invoke(provider);
    }

#pragma warning disable CS0618 // This builder deliberately implements only the published legacy contract.
    private sealed class LegacyOnlyBuilder(string name, IServiceCollection services) : IFusionGatewayBuilder
    {
        public string Name { get; } = name;

        public IServiceCollection Services { get; } = services;
    }
#pragma warning restore CS0618
}
