using HotChocolate.Execution.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public class TransportCapabilitiesProviderTests
{
    [Fact]
    public void GetCapabilities_Should_DeclareNoBatching_When_ServerOptionsAllowNoBatching()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQLServer("a");
        var provider = GetProvider(services);

        // act
        var capabilities = provider.GetCapabilities("a");

        // assert
        Assert.Equal(
            new TransportCapabilities(VariableBatching: false, RequestBatching: false),
            capabilities);
    }

    [Fact]
    public void GetCapabilities_Should_DeclareBothBatchingModes_When_SourceSchemaDefaultsApplied()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQLServer("a").AddSourceSchemaDefaults();
        var provider = GetProvider(services);

        // act
        var capabilities = provider.GetCapabilities("a");

        // assert
        Assert.Equal(
            new TransportCapabilities(VariableBatching: true, RequestBatching: true),
            capabilities);
    }

    [Fact]
    public void GetCapabilities_Should_MapEachFlagSeparately_When_OnlyVariableBatchingIsAllowed()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLServer("a")
            .ModifyServerOptions(o => o.Batching = AllowedBatching.VariableBatching);
        var provider = GetProvider(services);

        // act
        var capabilities = provider.GetCapabilities("a");

        // assert
        Assert.Equal(
            new TransportCapabilities(VariableBatching: true, RequestBatching: false),
            capabilities);
    }

    [Fact]
    public void GetCapabilities_Should_ReadNamedSchemaOptions_When_MultipleSchemasAreRegistered()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQLServer("a").AddSourceSchemaDefaults();
        services.AddGraphQLServer("b");
        var provider = GetProvider(services);

        // act
        var capabilities = provider.GetCapabilities("b");

        // assert
        Assert.Equal(
            new TransportCapabilities(VariableBatching: false, RequestBatching: false),
            capabilities);
    }

    private static ITransportCapabilitiesProvider GetProvider(IServiceCollection services)
        => services.BuildServiceProvider().GetRequiredService<ITransportCapabilitiesProvider>();
}
