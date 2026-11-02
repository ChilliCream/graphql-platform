using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Routing;

public class RabbitMQTransportResolverWiringTests
{
    [Fact]
    public void CreateEndpointConfiguration_Should_UseConventionExchangeName_When_DestinationNotConfigured()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddMessage<OrderCreated>(d => d.Publish(_ => { })));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var expectedName = runtime.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act
        var endpoint = transport.DispatchEndpoints
            .OfType<RabbitMQDispatchEndpoint>()
            .FirstOrDefault(e => e.Destination is RabbitMQExchange ex && ex.Name == expectedName);

        // assert
        Assert.NotNull(endpoint);
        Assert.Equal("e/" + expectedName, endpoint.Name);
        Assert.IsType<RabbitMQExchange>(endpoint.Destination);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_UseExplicitExchangeName_When_ExplicitExchangeDestinationConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d => d.Publish(r => r.ToRabbitMQExchange("orders-exchange"))));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.DispatchEndpoints
            .OfType<RabbitMQDispatchEndpoint>()
            .FirstOrDefault(e => e.Destination is RabbitMQExchange ex && ex.Name == "orders-exchange");

        // assert
        Assert.NotNull(endpoint);
        Assert.Equal("e/orders-exchange", endpoint.Name);
        Assert.IsType<RabbitMQExchange>(endpoint.Destination);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_UseExplicitQueueName_When_ExplicitQueueDestinationConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<ProcessPayment>(d => d.Send(r => r.ToRabbitMQQueue("orders-queue"))));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.DispatchEndpoints
            .OfType<RabbitMQDispatchEndpoint>()
            .FirstOrDefault(e => e.Destination is RabbitMQQueue q && q.Name == "orders-queue");

        // assert
        Assert.NotNull(endpoint);
        Assert.Equal("q/orders-queue", endpoint.Name);
        Assert.IsType<RabbitMQQueue>(endpoint.Destination);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        return builder
            .AddRabbitMQ(t => t.ConnectionProvider(_ => new StubConnectionProvider()))
            .BuildRuntime();
    }
}
