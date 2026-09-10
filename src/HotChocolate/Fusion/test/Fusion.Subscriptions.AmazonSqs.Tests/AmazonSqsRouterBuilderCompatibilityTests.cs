using System.Reflection;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Subscriptions.AmazonSqs;

public class AmazonSqsRouterBuilderCompatibilityTests
{
    // Reflection intentionally inspects the obsolete IFusionGatewayBuilder type.
#pragma warning disable CS0618
    [Fact]
    public void GatewayOverloads_Should_BeObsolete_When_RouterOverloadsAreAdded()
    {
        // arrange
        var type = typeof(AmazonSqsEventStreamBrokerServiceCollectionExtensions);
        var legacyMethods = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "AddAmazonSqsEventStreamBroker" && m.GetParameters()[0].ParameterType == typeof(IFusionGatewayBuilder))
            .ToArray();
        var routerMethods = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "AddAmazonSqsEventStreamBroker" && m.GetParameters()[0].ParameterType == typeof(IFusionRouterBuilder))
            .ToArray();

        // act & assert
        Assert.Equal(2, legacyMethods.Length);
        Assert.All(legacyMethods, m =>
        {
            var obsolete = Assert.Single(m.GetCustomAttributes<ObsoleteAttribute>());
            Assert.Equal("Use AddAmazonSqsEventStreamBroker on IFusionRouterBuilder instead.", obsolete.Message);
        });

        Assert.Equal(2, routerMethods.Length);
        Assert.All(routerMethods, m => Assert.Empty(m.GetCustomAttributes<ObsoleteAttribute>()));

        Assert.Equal(
            legacyMethods.Select(m => m.GetParameters().Length).Order(),
            routerMethods.Select(m => m.GetParameters().Length).Order());
    }
#pragma warning restore CS0618

    [Fact]
    public void AddAmazonSqsEventStreamBroker_Should_RegisterProviderOnce_When_UsingRouterChain_LegacyGatewayCall_And_LegacyOnlyBuilder()
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
        router.AddAmazonSqsEventStreamBroker("named", _ => routerInvocations++);
#pragma warning disable CS0618 // Intentional legacy static call.
        AmazonSqsEventStreamBrokerServiceCollectionExtensions.AddAmazonSqsEventStreamBroker(legacy, "named", _ => legacyInvocations++);
        AmazonSqsEventStreamBrokerServiceCollectionExtensions.AddAmazonSqsEventStreamBroker(customBuilder, "named", _ => customInvocations++);
#pragma warning restore CS0618

        // assert
        // Resolving the keyed provider would require valid broker connection options, so
        // registration is verified through the service descriptor and the named options
        // pipeline instead, without connecting to a real broker.
        Assert.Single(routerServices, d =>
            d.ServiceType == typeof(IEventStreamBrokerProvider) && Equals(d.ServiceKey, "named"));
        Assert.Single(legacyServices, d =>
            d.ServiceType == typeof(IEventStreamBrokerProvider) && Equals(d.ServiceKey, "named"));
        Assert.Single(customServices, d =>
            d.ServiceType == typeof(IEventStreamBrokerProvider) && Equals(d.ServiceKey, "named"));

        using var routerProvider = routerServices.BuildServiceProvider();
        using var legacyProvider = legacyServices.BuildServiceProvider();
        using var customProvider = customServices.BuildServiceProvider();

        // resolving the named options is what lazily triggers the configure callback exactly once
        routerProvider.GetRequiredService<IOptionsMonitor<AmazonSqsEventStreamOptions>>().Get("named");
        legacyProvider.GetRequiredService<IOptionsMonitor<AmazonSqsEventStreamOptions>>().Get("named");
        customProvider.GetRequiredService<IOptionsMonitor<AmazonSqsEventStreamOptions>>().Get("named");

        Assert.Equal(1, routerInvocations);
        Assert.Equal(1, legacyInvocations);
        Assert.Equal(1, customInvocations);
    }

    [Fact]
    public void AddAmazonSqsEventStreamBroker_Should_RegisterDefaultBroker_When_NameIsOmitted()
    {
        // arrange
        var services = new ServiceCollection();
        var router = services.AddGraphQLRouterCore();

        // act
        router.AddAmazonSqsEventStreamBroker();

        // assert
        Assert.Single(services, d =>
            d.ServiceType == typeof(IEventStreamBrokerProvider)
            && Equals(d.ServiceKey, DefaultEventStreamBrokerFactory.DefaultBrokerKey));
    }

#pragma warning disable CS0618 // This builder deliberately implements only the published legacy contract.
    private sealed class LegacyOnlyBuilder(string name, IServiceCollection services) : IFusionGatewayBuilder
    {
        public string Name { get; } = name;

        public IServiceCollection Services { get; } = services;
    }
#pragma warning restore CS0618
}
