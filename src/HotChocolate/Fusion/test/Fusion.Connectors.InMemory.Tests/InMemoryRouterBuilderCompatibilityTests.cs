using System.Reflection;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion;

public sealed class InMemoryRouterBuilderCompatibilityTests
{
    // Reflection intentionally inspects the obsolete IFusionGatewayBuilder type.
#pragma warning disable CS0618
    [Fact]
    public void GatewayExtensions_Should_BeObsolete_When_RouterExtensionsAreAdded()
    {
        // arrange
        var legacyType = typeof(InMemoryFusionGatewayBuilderExtensions);
        var routerType = typeof(InMemoryFusionRouterBuilderExtensions);
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
    public void ModifyInMemoryCompositionOptions_Should_ConfigureOptionsOnce_When_UsingRouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
    {
        // arrange
        var routerInvocations = 0;
        var routerServices = new ServiceCollection();
        var router = routerServices.AddGraphQLRouterCore("router-schema");

        var legacyInvocations = 0;
        var legacyServices = new ServiceCollection();
#pragma warning disable CS0618 // Intentional legacy static call.
        var legacy = legacyServices.AddGraphQLGateway("legacy-schema");
#pragma warning restore CS0618

        var customInvocations = 0;
        var customServices = new ServiceCollection();
#pragma warning disable CS0618 // Models a third-party legacy-only builder.
        var customBuilder = new LegacyOnlyBuilder("custom-schema", customServices);
#pragma warning restore CS0618

        // act
        router.ModifyInMemoryCompositionOptions(_ => routerInvocations++);
#pragma warning disable CS0618 // Intentional legacy static call.
        InMemoryFusionGatewayBuilderExtensions.ModifyInMemoryCompositionOptions(legacy, _ => legacyInvocations++);
        InMemoryFusionGatewayBuilderExtensions.ModifyInMemoryCompositionOptions(customBuilder, _ => customInvocations++);
#pragma warning restore CS0618

        // assert
        using var routerProvider = routerServices.BuildServiceProvider();
        using var legacyProvider = legacyServices.BuildServiceProvider();
        using var customProvider = customServices.BuildServiceProvider();

        // resolving Value is what lazily triggers the unnamed configure callback exactly once
        Assert.NotNull(routerProvider.GetRequiredService<IOptions<SchemaComposerOptions>>().Value);
        Assert.NotNull(legacyProvider.GetRequiredService<IOptions<SchemaComposerOptions>>().Value);
        Assert.NotNull(customProvider.GetRequiredService<IOptions<SchemaComposerOptions>>().Value);

        Assert.Equal(1, routerInvocations);
        Assert.Equal(1, legacyInvocations);
        Assert.Equal(1, customInvocations);
    }

    [Fact]
    public void AddInMemorySchema_Should_RegisterClientFactoryOnce_When_MixingRouterAndLegacyCallsOnSameBuilder()
    {
        // arrange
        var services = new ServiceCollection();
        var router = services.AddGraphQLRouterCore("shared-schema");

        // act
        router.AddInMemorySchema("schema-a");
#pragma warning disable CS0618 // Intentional legacy static call against the same underlying router builder.
        InMemoryFusionGatewayBuilderExtensions.AddInMemorySchema(router, "schema-b");
#pragma warning restore CS0618

        // assert
        Assert.Single(services, d => d.ServiceType == typeof(ISourceSchemaClientFactory));
    }

    [Fact]
    public void AddInMemorySchema_Should_UseExecutorBuilderName_When_PassedARequestExecutorBuilder()
    {
        // arrange
        var services = new ServiceCollection();
        var schemaBuilder = services.AddGraphQL("products");
        var router = services.AddGraphQLRouterCore();

        // act
        router.AddInMemorySchema(schemaBuilder);

        // assert
        Assert.Single(services, d => d.ServiceType == typeof(ISourceSchemaClientFactory));
    }

#pragma warning disable CS0618 // This builder deliberately implements only the published legacy contract.
    private sealed class LegacyOnlyBuilder(string name, IServiceCollection services) : IFusionGatewayBuilder
    {
        public string Name { get; } = name;

        public IServiceCollection Services { get; } = services;
    }
#pragma warning restore CS0618
}
