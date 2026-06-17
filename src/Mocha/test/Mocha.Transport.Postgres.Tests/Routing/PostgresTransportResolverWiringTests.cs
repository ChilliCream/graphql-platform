using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Routing;

/// <summary>
/// Verifies that <see cref="PostgresMessagingTransport.CreateEndpointConfiguration(IMessagingConfigurationContext, OutboundRoute)"/>
/// uses the internal <see cref="PostgresDestinations"/> helper and produces the correct
/// dispatch endpoint configuration for convention and explicit destinations.
/// </summary>
public class PostgresTransportResolverWiringTests
{
    [Fact]
    public void CreateEndpointConfiguration_Should_UseConventionTopicName_When_DestinationNotConfigured()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddMessage<OrderCreated>(d => d.Publish(_ => { })));
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var expectedName = runtime.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act
        var endpoint = transport.DispatchEndpoints
            .OfType<PostgresDispatchEndpoint>()
            .FirstOrDefault(e => e.Destination is PostgresTopic t && t.Name == expectedName);

        // assert
        Assert.NotNull(endpoint);
        Assert.Equal("t/" + expectedName, endpoint.Name);
        Assert.IsType<PostgresTopic>(endpoint.Destination);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_UseExplicitTopicName_When_ExplicitTopicDestinationConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d => d.Publish(r => r.ToPostgresTopic("orders-topic"))));
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport.DispatchEndpoints
            .OfType<PostgresDispatchEndpoint>()
            .FirstOrDefault(e => e.Destination is PostgresTopic t && t.Name == "orders-topic");

        // assert
        Assert.NotNull(endpoint);
        Assert.Equal("t/orders-topic", endpoint.Name);
        Assert.IsType<PostgresTopic>(endpoint.Destination);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        return builder
            .AddPostgres(t => t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test"))
            .BuildRuntime();
    }
}
