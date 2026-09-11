using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Compatibility;

/// <summary>
/// Exercises the current (locally built) product assemblies from a non-friend consumer: no
/// InternalsVisibleTo access exists anywhere for HotChocolate.Fusion.Compatibility.Tests, so
/// every call below goes through the same public surface a real external consumer would use.
/// </summary>
public sealed class RuntimeBridgeTests
{
    [Fact]
    public void AddGraphQLGateway_Should_ReturnBuilderWrappingSameServices_When_UsingDefaultAndNamedNonDefaultArguments()
    {
        // arrange
        var defaultServices = new ServiceCollection();
        var namedServices = new ServiceCollection();
        var serverServices = new ServiceCollection();

        // act
#pragma warning disable CS0618 // Intentional legacy compatibility calls under proof.
        var defaultBuilder = defaultServices.AddGraphQLGateway();
        var namedBuilder = namedServices.AddGraphQLGateway(name: "named-schema");
        var serverBuilder = serverServices.AddGraphQLGatewayServer(
            name: "server-schema",
            maxAllowedRequestSize: 2_000_000,
            disableDefaultSecurity: true);
#pragma warning restore CS0618

        // assert
        Assert.Equal(ISchemaDefinition.DefaultName, defaultBuilder.Name);
        Assert.Same(defaultServices, defaultBuilder.Services);
        Assert.Equal("named-schema", namedBuilder.Name);
        Assert.Equal("server-schema", serverBuilder.Name);
    }

    [Fact]
    public void AddGraphQLGateway_Should_ReturnBuilderWrappingHostServices_When_UsingIHostApplicationBuilder()
    {
        // arrange
        var hostBuilder = new TestHostApplicationBuilder();

        // act
#pragma warning disable CS0618 // Intentional legacy compatibility call under proof.
        var gatewayBuilder = hostBuilder.AddGraphQLGateway(
            name: "host-schema",
            maxAllowedRequestSize: 111_222,
            disableDefaultSecurity: false);
#pragma warning restore CS0618

        // assert
        Assert.Equal("host-schema", gatewayBuilder.Name);
        Assert.Same(hostBuilder.Services, gatewayBuilder.Services);
    }

    [Fact]
    public void LegacyStaticCall_Should_ReturnOriginalReceiver_When_CalledAsDeclaringClassStaticMethod()
    {
        // arrange
        var services = new ServiceCollection();
#pragma warning disable CS0618 // Intentional legacy compatibility call under proof.
        var builder = services.AddGraphQLGatewayServer(name: "identity-schema");

        // act: declaring-class static call syntax, not extension-method syntax.
        var afterCacheControl = FusionCachingGatewayBuilderExtensions.AddCacheControl(builder);
        var afterQueryCache = FusionCachingGatewayBuilderExtensions.UseQueryCache(afterCacheControl, after: null);
#pragma warning restore CS0618

        // assert
        Assert.Same(builder, afterCacheControl);
        Assert.Same(builder, afterQueryCache);
    }

    [Fact]
    public void RouterBridgeExtension_Should_ReturnOriginalReceiver_When_ForwardingToLegacyImplementation()
    {
        // arrange
        var services = new ServiceCollection();
        var router = services.AddGraphQLRouterCore("bridge-schema");

        // act: the new router-named extension forwards to the legacy static implementation.
        var afterCacheControl = router.AddCacheControl();
        var afterQueryCache = afterCacheControl.UseQueryCache();

        // assert
        Assert.Same(router, afterCacheControl);
        Assert.Same(router, afterQueryCache);
    }

    [Fact]
    public void CustomLegacyOnlyBuilder_Should_FlowThroughLegacyAndThirdPartyExtensions_When_NotImplementingRouterBuilder()
    {
        // arrange
        var services = new ServiceCollection();
        var custom = new CustomLegacyBuilder("custom-schema", services);

        // act
#pragma warning disable CS0618 // Models a third-party legacy-only builder under proof.
        var afterCacheControl = FusionCachingGatewayBuilderExtensions.AddCacheControl(custom);
#pragma warning restore CS0618
        var afterThirdParty = ThirdPartyGatewayExtensions.Configure(afterCacheControl);

        // assert
        Assert.False((object)custom is IFusionRouterBuilder);
        Assert.Same(custom, afterCacheControl);
        Assert.Same(custom, afterThirdParty);
    }

    [Fact]
    public void ModifyInMemoryCompositionOptions_Should_RunCallbacksInRegistrationOrder_When_MixingRouterAndLegacyCallsOnSameBuilder()
    {
        // arrange
        var order = new List<string>();
        var services = new ServiceCollection();
        var router = services.AddGraphQLRouterCore("ordering-schema");

        // act
        router.ModifyInMemoryCompositionOptions(_ => order.Add("router"));
#pragma warning disable CS0618 // Intentional legacy compatibility call under proof.
        InMemoryFusionGatewayBuilderExtensions.ModifyInMemoryCompositionOptions(router, _ => order.Add("legacy"));
#pragma warning restore CS0618

        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IOptions<SchemaComposerOptions>>().Value;

        // assert
        Assert.Equal(["router", "legacy"], order);
    }

    [Fact]
    public async Task IRequestExecutorProvider_Should_ExposeBothSchemaNames_When_TwoRouterBuildersShareOneServiceCollection()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQLRouterCore("schema-a");
        services.AddGraphQLRouterCore("schema-b");

        // act
        await using var provider = services.BuildServiceProvider();
        var schemaNames = provider.GetRequiredService<IRequestExecutorProvider>().SchemaNames
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // assert
        Assert.Equal(["schema-a", "schema-b"], schemaNames);
    }

    [Fact]
    public async Task IRequestExecutorProvider_Should_ExposeSchemaName_When_RegisteredThroughEitherEntryPoint()
    {
        // arrange
        var legacyServices = new ServiceCollection();
        var routerServices = new ServiceCollection();
#pragma warning disable CS0618 // Intentional legacy compatibility call under proof.
        legacyServices.AddGraphQLGatewayServer(name: "legacy-provider-schema");
#pragma warning restore CS0618
        routerServices.AddGraphQLRouter(name: "router-provider-schema");

        // act
        await using var legacyProvider = legacyServices.BuildServiceProvider();
        await using var routerProvider = routerServices.BuildServiceProvider();
        var legacyNames = legacyProvider.GetRequiredService<IRequestExecutorProvider>().SchemaNames;
        var routerNames = routerProvider.GetRequiredService<IRequestExecutorProvider>().SchemaNames;

        // assert
        Assert.Equal(["legacy-provider-schema"], legacyNames);
        Assert.Equal(["router-provider-schema"], routerNames);
    }

    // The obsolete/router-clean shape of each family is proved once, structurally, by
    // CompatibilitySurfaceTests; the two tests below are registration-only proofs that the
    // five broker families and the two adapter families actually flow through the router
    // chain, a legacy gateway call and a legacy-only third-party builder, without a real broker
    // or resolving anything beyond public DI/options APIs.
    [Fact]
    public void BrokerFamilies_Should_RegisterKeyedProvider_When_UsingRouterChainAndLegacyOnlyBuilder()
    {
        // arrange
#pragma warning disable CS0618 // Intentional legacy declaring-class static calls under proof.
        (string Name, Action<IFusionRouterBuilder> AddToRouter, Action<CustomLegacyBuilder> AddToLegacyBuilder)[] families =
        [
            (
                "NATS",
                b => b.AddNatsEventStreamBroker(name: "broker"),
                b => NatsEventStreamBrokerServiceCollectionExtensions.AddNatsEventStreamBroker(b, name: "broker")),
            (
                "Kafka",
                b => b.AddKafkaEventStreamBroker(name: "broker"),
                b => KafkaEventStreamBrokerServiceCollectionExtensions.AddKafkaEventStreamBroker(b, name: "broker")),
            (
                "Redis",
                b => b.AddRedisEventStreamBroker(name: "broker"),
                b => RedisEventStreamBrokerServiceCollectionExtensions.AddRedisEventStreamBroker(b, name: "broker")),
            (
                "AmazonSqs",
                b => b.AddAmazonSqsEventStreamBroker(name: "broker"),
                b => AmazonSqsEventStreamBrokerServiceCollectionExtensions.AddAmazonSqsEventStreamBroker(b, name: "broker")),
            (
                "AzureEventHubs",
                b => b.AddAzureEventHubsEventStreamBroker(name: "broker"),
                b => AzureEventHubsEventStreamBrokerServiceCollectionExtensions.AddAzureEventHubsEventStreamBroker(b, name: "broker"))
        ];
#pragma warning restore CS0618

        // act
        var report = families.Select(family =>
        {
            var routerServices = new ServiceCollection();
            var router = routerServices.AddGraphQLRouterCore($"{family.Name}-router-schema");
            family.AddToRouter(router);

            var legacyServices = new ServiceCollection();
            var legacyBuilder = new CustomLegacyBuilder($"{family.Name}-legacy-schema", legacyServices);
            family.AddToLegacyBuilder(legacyBuilder);

            return new
            {
                family.Name,
                RouterProviderRegistered = IsBrokerProviderRegistered(routerServices),
                LegacyOnlyBuilderProviderRegistered = IsBrokerProviderRegistered(legacyServices)
            };
        }).ToArray();

        // assert
        report.MatchInlineSnapshot(
            """
            [
              {
                "Name": "NATS",
                "RouterProviderRegistered": true,
                "LegacyOnlyBuilderProviderRegistered": true
              },
              {
                "Name": "Kafka",
                "RouterProviderRegistered": true,
                "LegacyOnlyBuilderProviderRegistered": true
              },
              {
                "Name": "Redis",
                "RouterProviderRegistered": true,
                "LegacyOnlyBuilderProviderRegistered": true
              },
              {
                "Name": "AmazonSqs",
                "RouterProviderRegistered": true,
                "LegacyOnlyBuilderProviderRegistered": true
              },
              {
                "Name": "AzureEventHubs",
                "RouterProviderRegistered": true,
                "LegacyOnlyBuilderProviderRegistered": true
              }
            ]
            """);
    }

    private static bool IsBrokerProviderRegistered(IServiceCollection services)
        => services.Any(d =>
            d.ServiceType == typeof(IEventStreamBrokerProvider) && Equals(d.ServiceKey, "broker"));

    [Fact]
    public void AdapterFamilies_Should_PreserveReceiverIdentityAndModifierCount_When_UsingRouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
    {
        // arrange
#pragma warning disable CS0618 // Intentional legacy declaring-class static calls under proof.
        (string Name, Func<IFusionRouterBuilder, IFusionRouterBuilder> AddToRouter, Func<IFusionGatewayBuilder, IFusionGatewayBuilder> AddToGatewayBuilder)[] families =
        [
            ("Mcp", b => b.AddMcp(), b => FusionGatewayBuilderExtensions.AddMcp(b)),
            ("OpenApi", b => b.AddOpenApi(), b => OpenApiFusionGatewayBuilderExtensions.AddOpenApi(b))
        ];
#pragma warning restore CS0618

        // act
        var report = families.Select(family =>
        {
            var routerServices = new ServiceCollection();
            var router = routerServices.AddGraphQLRouterCore($"{family.Name}-router-schema");
            var routerResult = family.AddToRouter(router);

            var legacyServices = new ServiceCollection();
#pragma warning disable CS0618 // Intentional legacy static call under proof.
            var legacy = legacyServices.AddGraphQLGateway($"{family.Name}-legacy-schema");
#pragma warning restore CS0618
            var legacyResult = family.AddToGatewayBuilder(legacy);

            var customServices = new ServiceCollection();
#pragma warning disable CS0618 // Models a third-party legacy-only builder under proof.
            var custom = new CustomLegacyBuilder($"{family.Name}-custom-schema", customServices);
#pragma warning restore CS0618
            var customResult = family.AddToGatewayBuilder(custom);

            using var routerProvider = routerServices.BuildServiceProvider();
            using var legacyProvider = legacyServices.BuildServiceProvider();
            using var customProvider = customServices.BuildServiceProvider();

            return new
            {
                family.Name,
                RouterIsOriginalReceiver = ReferenceEquals(router, routerResult),
                LegacyIsOriginalReceiver = ReferenceEquals(legacy, legacyResult),
                CustomIsOriginalReceiver = ReferenceEquals(custom, customResult),
                RouterModifierCount = GetSchemaServiceModifierCount(routerProvider, $"{family.Name}-router-schema"),
                LegacyModifierCount = GetSchemaServiceModifierCount(legacyProvider, $"{family.Name}-legacy-schema"),
                CustomModifierCount = GetSchemaServiceModifierCount(customProvider, $"{family.Name}-custom-schema")
            };
        }).ToArray();

        // assert
        report.MatchInlineSnapshot(
            """
            [
              {
                "Name": "Mcp",
                "RouterIsOriginalReceiver": true,
                "LegacyIsOriginalReceiver": true,
                "CustomIsOriginalReceiver": true,
                "RouterModifierCount": 4,
                "LegacyModifierCount": 4,
                "CustomModifierCount": 2
              },
              {
                "Name": "OpenApi",
                "RouterIsOriginalReceiver": true,
                "LegacyIsOriginalReceiver": true,
                "CustomIsOriginalReceiver": true,
                "RouterModifierCount": 4,
                "LegacyModifierCount": 4,
                "CustomModifierCount": 2
              }
            ]
            """);
    }

    private static int GetSchemaServiceModifierCount(ServiceProvider provider, string schemaName)
        => provider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>()
            .Get(schemaName)
            .SchemaServiceModifiers
            .Count;
}

// Models a third-party legacy-only builder and extension, both written against the old,
// published interface, from outside the product assemblies.
#pragma warning disable CS0618
file sealed class CustomLegacyBuilder(string name, IServiceCollection services) : IFusionGatewayBuilder
{
    public string Name { get; } = name;

    public IServiceCollection Services { get; } = services;
}

file static class ThirdPartyGatewayExtensions
{
    public static IFusionGatewayBuilder Configure(this IFusionGatewayBuilder builder) => builder;
}
#pragma warning restore CS0618
