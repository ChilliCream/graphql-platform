using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Routing;

public class RabbitMQDestinationsTests
{
    [Fact]
    public void ResolveDestination_Should_UseConventionExchange_When_DestinationNotConfigured()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddMessage<OrderCreated>(d => d.Publish(_ => { })));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();
        var expectedName = runtime.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act
        var resolution = RabbitMQDestinations.Resolve(
            RabbitMQTransportConfiguration.DefaultSchema,
            runtime.Naming,
            route);

        // assert
        Assert.Equal(RabbitMQDestinationKind.Exchange, resolution.Kind);
        Assert.Equal(expectedName, resolution.Name);
        Assert.Equal("e/" + expectedName, resolution.EndpointName);
    }

    [Fact]
    public void ResolveDestination_Should_ResolveExplicitExchange_When_DestinationIsExchange()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d => d.Publish(r => r.ToRabbitMQExchange("orders-exchange"))));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();

        // act
        var resolution = RabbitMQDestinations.Resolve(
            RabbitMQTransportConfiguration.DefaultSchema,
            runtime.Naming,
            route);

        // assert
        Assert.Equal(RabbitMQDestinationKind.Exchange, resolution.Kind);
        Assert.Equal("orders-exchange", resolution.Name);
        Assert.Equal("e/orders-exchange", resolution.EndpointName);
    }

    [Fact]
    public void ResolveDestination_Should_ResolveExplicitQueue_When_DestinationIsQueue()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d => d.Send(r => r.ToRabbitMQQueue("orders-queue"))));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();

        // act
        var resolution = RabbitMQDestinations.Resolve(
            RabbitMQTransportConfiguration.DefaultSchema,
            runtime.Naming,
            route);

        // assert
        Assert.Equal(RabbitMQDestinationKind.Queue, resolution.Kind);
        Assert.Equal("orders-queue", resolution.Name);
        Assert.Equal("q/orders-queue", resolution.EndpointName);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddRabbitMQ(t => t.ConnectionProvider(_ => new StubConnectionProvider()))
            .BuildRuntime();
        return runtime;
    }
}
