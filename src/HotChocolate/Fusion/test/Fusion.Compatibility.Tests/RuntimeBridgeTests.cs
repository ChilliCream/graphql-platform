using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Options;
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

    [Fact]
    public void LegacyExtensionFamilies_Should_BeObsoleteWithNameMatchingRouterEquivalents_When_ReflectedFromNonFriendAssembly()
    {
        // arrange
#pragma warning disable CS0618 // Reflecting over the obsolete legacy family types themselves.
        var families = new (Type Legacy, Type Router)[]
        {
            (typeof(CoreFusionGatewayBuilderExtensions), typeof(CoreFusionRouterBuilderExtensions)),
            (typeof(FusionCachingGatewayBuilderExtensions), typeof(FusionCachingRouterBuilderExtensions)),
            (typeof(DiagnosticsFusionGatewayBuilderExtensions), typeof(DiagnosticsFusionRouterBuilderExtensions)),
            (typeof(InMemoryFusionGatewayBuilderExtensions), typeof(InMemoryFusionRouterBuilderExtensions)),
            (typeof(AspNetCoreFusionGatewayBuilderExtensions), typeof(AspNetCoreFusionRouterBuilderExtensions))
        };
#pragma warning restore CS0618

        // act
        var report = families.Select(DescribeFamily).ToArray();

        // assert
        report.MatchInlineSnapshot(
            """
            [
              "CoreFusionGatewayBuilderExtensions: legacyMethodCount=66, namesMatchRouter=True, allLegacyObsoleteOnGatewayBuilder=True, allRouterCleanOnRouterBuilder=True",
              "FusionCachingGatewayBuilderExtensions: legacyMethodCount=3, namesMatchRouter=True, allLegacyObsoleteOnGatewayBuilder=True, allRouterCleanOnRouterBuilder=True",
              "DiagnosticsFusionGatewayBuilderExtensions: legacyMethodCount=2, namesMatchRouter=True, allLegacyObsoleteOnGatewayBuilder=True, allRouterCleanOnRouterBuilder=True",
              "InMemoryFusionGatewayBuilderExtensions: legacyMethodCount=3, namesMatchRouter=True, allLegacyObsoleteOnGatewayBuilder=True, allRouterCleanOnRouterBuilder=True",
              "AspNetCoreFusionGatewayBuilderExtensions: legacyMethodCount=9, namesMatchRouter=True, allLegacyObsoleteOnGatewayBuilder=True, allRouterCleanOnRouterBuilder=True"
            ]
            """);
    }

#pragma warning disable CS0618 // Reflecting over the obsolete IFusionGatewayBuilder type itself.
    private static string DescribeFamily((Type Legacy, Type Router) family)
    {
        var legacyMethods = family.Legacy
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !m.IsSpecialName)
            .OrderBy(m => m.Name)
            .ToArray();
        var routerMethods = family.Router
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !m.IsSpecialName)
            .OrderBy(m => m.Name)
            .ToArray();

        var namesMatchRouter = legacyMethods.Select(m => m.Name)
            .SequenceEqual(routerMethods.Select(m => m.Name));

        var allLegacyObsoleteOnGatewayBuilder = legacyMethods.Length > 0 && legacyMethods.All(m =>
            m.GetParameters()[0].ParameterType == typeof(IFusionGatewayBuilder)
            && m.GetCustomAttribute<ObsoleteAttribute>() is not null);

        var allRouterCleanOnRouterBuilder = routerMethods.Length > 0 && routerMethods.All(m =>
            m.GetParameters()[0].ParameterType == typeof(IFusionRouterBuilder)
            && m.GetCustomAttribute<ObsoleteAttribute>() is null);

        return $"{family.Legacy.Name}: legacyMethodCount={legacyMethods.Length}, "
            + $"namesMatchRouter={namesMatchRouter}, "
            + $"allLegacyObsoleteOnGatewayBuilder={allLegacyObsoleteOnGatewayBuilder}, "
            + $"allRouterCleanOnRouterBuilder={allRouterCleanOnRouterBuilder}";
    }
#pragma warning restore CS0618
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
