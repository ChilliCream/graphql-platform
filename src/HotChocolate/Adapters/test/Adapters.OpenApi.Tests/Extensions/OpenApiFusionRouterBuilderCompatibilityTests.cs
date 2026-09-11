using System.Reflection;
using HotChocolate.Adapters.OpenApi.Configuration;
using HotChocolate.Adapters.OpenApi.Storage;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.Adapters.OpenApi.Extensions;

public sealed class OpenApiFusionRouterBuilderCompatibilityTests
{
    // Reflection intentionally inspects the obsolete IFusionGatewayBuilder type.
#pragma warning disable CS0618
    [Fact]
    public void GatewayExtensions_Should_BeObsolete_When_RouterExtensionsAreAdded()
    {
        // arrange
        var legacyType = typeof(OpenApiFusionGatewayBuilderExtensions);
        var routerType = typeof(OpenApiFusionRouterBuilderExtensions);
        var legacyMethods = legacyType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        var routerMethods = routerType.GetMethods(BindingFlags.Public | BindingFlags.Static);

        // act & assert
        Assert.Equal(4, legacyMethods.Length);
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
    public void AddOpenApi_Should_ConfigureSchemaServicesOnce_When_UsingRouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
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
        Assert.Same(router, router.AddOpenApi());
#pragma warning disable CS0618 // Intentional legacy static calls.
        Assert.Same(legacy, OpenApiFusionGatewayBuilderExtensions.AddOpenApi(legacy));
        Assert.Same(customBuilder, OpenApiFusionGatewayBuilderExtensions.AddOpenApi(customBuilder));
#pragma warning restore CS0618

        // assert
        // AddOpenApi registers one schema-service modifier for the result formatter/schema
        // services and, through AddWarmupTask, another for the definitions warmup task; each
        // surface should add exactly that, once, on top of what AddGraphQLRouterCore/
        // AddGraphQLGateway already registers.
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
    public void AddOpenApi_Should_RegisterWarmupTask_When_SkipIfFalse_And_Omit_When_SkipIfTrue()
    {
        // arrange
        var runServices = new ServiceCollection();
        var runBuilder = runServices.AddGraphQLRouterCore("run-schema");
        runBuilder.AddOpenApi(skipIf: _ => false);

        var skipServices = new ServiceCollection();
        var skipBuilder = skipServices.AddGraphQLRouterCore("skip-schema");
        skipBuilder.AddOpenApi(skipIf: _ => true);

        using var runProvider = runServices.BuildServiceProvider();
        using var skipProvider = skipServices.BuildServiceProvider();
        var runSetup = runProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("run-schema");
        var skipSetup = skipProvider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get("skip-schema");

        var runSchemaServices = new ServiceCollection();
        var skipSchemaServices = new ServiceCollection();

        // act
        // AddOpenApi registers the warmup modifier last, after the schema services one.
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
    public void AddOpenApiDefinitionStorage_Should_SetStorageFactoryOnce_When_UsingRouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
    {
        // arrange
        var storage = new StubOpenApiDefinitionStorage();

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
        Assert.Same(router, router.AddOpenApiDefinitionStorage(storage));
#pragma warning disable CS0618 // Intentional legacy static calls.
        Assert.Same(legacy, OpenApiFusionGatewayBuilderExtensions.AddOpenApiDefinitionStorage(legacy, storage));
        Assert.Same(customBuilder, OpenApiFusionGatewayBuilderExtensions.AddOpenApiDefinitionStorage(customBuilder, storage));
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
    public void AddOpenApiDefinitionStorage_Should_SetStorageFactoryOnce_When_UsingGenericOverload_Across_RouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
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
        Assert.Same(router, router.AddOpenApiDefinitionStorage<StubOpenApiDefinitionStorage>());
#pragma warning disable CS0618 // Intentional legacy static calls.
        Assert.Same(legacy, OpenApiFusionGatewayBuilderExtensions.AddOpenApiDefinitionStorage<StubOpenApiDefinitionStorage>(legacy));
        Assert.Same(customBuilder, OpenApiFusionGatewayBuilderExtensions.AddOpenApiDefinitionStorage<StubOpenApiDefinitionStorage>(customBuilder));
#pragma warning restore CS0618

        // assert
        using var routerProvider = routerServices.BuildServiceProvider();
        using var legacyProvider = legacyServices.BuildServiceProvider();
        using var customProvider = customServices.BuildServiceProvider();

        Assert.IsType<StubOpenApiDefinitionStorage>(ResolveStorage(routerProvider, "router-schema"));
        Assert.IsType<StubOpenApiDefinitionStorage>(ResolveStorage(legacyProvider, "legacy-schema"));
        Assert.IsType<StubOpenApiDefinitionStorage>(ResolveStorage(customProvider, "custom-schema"));
    }

    [Fact]
    public void AddOpenApiDefinitionStorage_Should_SetStorageFactoryOnce_When_UsingFactoryOverload_Across_RouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
    {
        // arrange
        var storage = new StubOpenApiDefinitionStorage();
        Func<IServiceProvider, IOpenApiDefinitionStorage> factory = _ => storage;

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
        Assert.Same(router, router.AddOpenApiDefinitionStorage(factory));
#pragma warning disable CS0618 // Intentional legacy static calls.
        Assert.Same(legacy, OpenApiFusionGatewayBuilderExtensions.AddOpenApiDefinitionStorage(legacy, factory));
        Assert.Same(customBuilder, OpenApiFusionGatewayBuilderExtensions.AddOpenApiDefinitionStorage(customBuilder, factory));
#pragma warning restore CS0618

        // assert
        using var routerProvider = routerServices.BuildServiceProvider();
        using var legacyProvider = legacyServices.BuildServiceProvider();
        using var customProvider = customServices.BuildServiceProvider();

        Assert.Same(storage, ResolveStorage(routerProvider, "router-schema"));
        Assert.Same(storage, ResolveStorage(legacyProvider, "legacy-schema"));
        Assert.Same(storage, ResolveStorage(customProvider, "custom-schema"));
    }

    private static IOpenApiDefinitionStorage? ResolveStorage(ServiceProvider provider, string schemaName)
    {
        var setup = provider.GetRequiredService<IOptionsMonitor<OpenApiSetup>>().Get(schemaName);
        return setup.StorageFactory?.Invoke(provider);
    }

#pragma warning disable CS0618 // This builder deliberately implements only the published legacy contract.
    private sealed class LegacyOnlyBuilder(string name, IServiceCollection services) : IFusionGatewayBuilder
    {
        public string Name { get; } = name;

        public IServiceCollection Services { get; } = services;
    }
#pragma warning restore CS0618

    private sealed class StubOpenApiDefinitionStorage : IOpenApiDefinitionStorage
    {
        public ValueTask<IEnumerable<IOpenApiDefinition>> GetDefinitionsAsync(
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IEnumerable<IOpenApiDefinition>>([]);

        public IDisposable Subscribe(IObserver<OpenApiDefinitionStorageEventArgs> observer)
            => EmptyDisposable.Instance;

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
