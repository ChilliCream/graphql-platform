using System.Reflection;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.FusionDiagnostics;

public class DiagnosticsRouterBuilderCompatibilityTests
{
    // Reflection intentionally inspects the obsolete IFusionGatewayBuilder type.
#pragma warning disable CS0618
    [Fact]
    public void GatewayOverloads_Should_BeObsolete_When_RouterOverloadsAreAdded()
    {
        // arrange
        var legacyType = typeof(DiagnosticsFusionGatewayBuilderExtensions);
        var routerType = typeof(DiagnosticsFusionRouterBuilderExtensions);
        var legacyMethods = legacyType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "AddInstrumentation")
            .ToArray();
        var routerMethods = routerType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "AddInstrumentation")
            .ToArray();

        // act & assert
        Assert.Equal(2, legacyMethods.Length);
        Assert.All(legacyMethods, m =>
        {
            Assert.Equal(typeof(IFusionGatewayBuilder), m.GetParameters()[0].ParameterType);
            var obsolete = Assert.Single(m.GetCustomAttributes<ObsoleteAttribute>());
            Assert.Equal("Use AddInstrumentation on IFusionRouterBuilder instead.", obsolete.Message);
        });

        Assert.Equal(2, routerMethods.Length);
        Assert.All(routerMethods, m =>
        {
            Assert.Equal(typeof(IFusionRouterBuilder), m.GetParameters()[0].ParameterType);
            Assert.Empty(m.GetCustomAttributes<ObsoleteAttribute>());
        });
    }
#pragma warning restore CS0618

    [Fact]
    public void AddInstrumentation_Should_ConfigureOptionsOnce_When_UsingRouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
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
        router.AddInstrumentation(_ => routerInvocations++);
#pragma warning disable CS0618 // Intentional legacy static call.
        DiagnosticsFusionGatewayBuilderExtensions.AddInstrumentation(legacy, _ => legacyInvocations++);
        DiagnosticsFusionGatewayBuilderExtensions.AddInstrumentation(customBuilder, _ => customInvocations++);
#pragma warning restore CS0618

        // assert
        // InstrumentationOptions is registered with TryAddSingleton, so resolving it once
        // per container is what proves the configure callback ran exactly once.
        using var routerProvider = routerServices.BuildServiceProvider();
        using var legacyProvider = legacyServices.BuildServiceProvider();
        using var customProvider = customServices.BuildServiceProvider();

        routerProvider.GetRequiredService<InstrumentationOptions>();
        legacyProvider.GetRequiredService<InstrumentationOptions>();
        customProvider.GetRequiredService<InstrumentationOptions>();

        Assert.Equal(1, routerInvocations);
        Assert.Equal(1, legacyInvocations);
        Assert.Equal(1, customInvocations);

        Assert.Single(routerServices, d => d.ServiceType == typeof(InstrumentationOptions));
        Assert.Single(legacyServices, d => d.ServiceType == typeof(InstrumentationOptions));
        Assert.Single(customServices, d => d.ServiceType == typeof(InstrumentationOptions));
    }

#pragma warning disable CS0618 // This builder deliberately implements only the published legacy contract.
    private sealed class LegacyOnlyBuilder(string name, IServiceCollection services) : IFusionGatewayBuilder
    {
        public string Name { get; } = name;

        public IServiceCollection Services { get; } = services;
    }
#pragma warning restore CS0618
}
