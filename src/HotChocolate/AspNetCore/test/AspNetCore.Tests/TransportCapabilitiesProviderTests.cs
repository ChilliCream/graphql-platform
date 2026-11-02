using HotChocolate.Execution.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public class TransportCapabilitiesProviderTests
{
    [Fact]
    public void GetCapabilities_Should_DeclareOnlyVariableBatching_When_ServerOptionsAreDefault()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQLServer("a");
        var provider = GetProvider(services);

        // act
        var capabilities = provider.GetCapabilities("a");

        // assert
        Assert.Equal(
            new TransportCapabilities(VariableBatching: true, RequestBatching: false),
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

    [Theory]
    [InlineData(AllowedBatching.None, false, false)]
    [InlineData(AllowedBatching.VariableBatching, true, false)]
    [InlineData(AllowedBatching.RequestBatching, false, true)]
    [InlineData(AllowedBatching.All, true, true)]
    public void GetCapabilities_Should_MapEachFlagSeparately_When_BatchingIsConfigured(
        AllowedBatching batching,
        bool variableBatching,
        bool requestBatching)
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLServer("a")
            .ModifyServerOptions(o => o.Batching = batching);
        var provider = GetProvider(services);

        // act
        var capabilities = provider.GetCapabilities("a");

        // assert
        Assert.Equal(
            new TransportCapabilities(VariableBatching: variableBatching, RequestBatching: requestBatching),
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
            new TransportCapabilities(VariableBatching: true, RequestBatching: false),
            capabilities);
    }

    private static ITransportCapabilitiesProvider GetProvider(IServiceCollection services)
        => services.BuildServiceProvider().GetRequiredService<ITransportCapabilitiesProvider>();
}
