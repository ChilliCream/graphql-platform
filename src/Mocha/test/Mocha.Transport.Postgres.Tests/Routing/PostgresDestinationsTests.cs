using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Routing;

public class PostgresDestinationsTests
{
    [Fact]
    public void ResolveDestination_Should_UseConventionTopic_When_DestinationNotConfigured()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddMessage<OrderCreated>(d => d.Publish(_ => { })));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();
        var expectedName = runtime.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act
        var resolution = PostgresDestinations.Resolve(
            PostgresTransportConfiguration.DefaultSchema,
            runtime.Naming,
            route);

        // assert
        Assert.Equal(PostgresDestinationKind.Topic, resolution.Kind);
        Assert.Equal(expectedName, resolution.Name);
        Assert.Equal("t/" + expectedName, resolution.EndpointName);
    }

    [Fact]
    public void ResolveDestination_Should_ResolveExplicitTopic_When_DestinationIsTopic()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d => d.Publish(r => r.ToPostgresTopic("orders-topic"))));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();

        // act
        var resolution = PostgresDestinations.Resolve(
            PostgresTransportConfiguration.DefaultSchema,
            runtime.Naming,
            route);

        // assert
        Assert.Equal(PostgresDestinationKind.Topic, resolution.Kind);
        Assert.Equal("orders-topic", resolution.Name);
        Assert.Equal("t/orders-topic", resolution.EndpointName);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddPostgres(t => t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test"))
            .BuildRuntime();
        return runtime;
    }
}
