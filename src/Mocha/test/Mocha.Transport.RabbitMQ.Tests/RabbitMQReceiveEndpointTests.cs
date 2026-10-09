using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

public class RabbitMQReceiveEndpointTests
{
    [Fact]
    public void ReceiveEndpoint_Should_ResolveQueue_When_QueueNameConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
            t.Queue("my-q").AutoProvision().Handler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().First(e => e.Queue.Name == "my-q");

        // assert
        Assert.Equal("my-q", endpoint.Queue.Name);
        Assert.IsType<RabbitMQQueue>(endpoint.Source);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetFaultEndpointDestinationName_When_FaultEndpointConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
            t.Queue("q").AutoProvision().Handler<OrderCreatedHandler>().FaultEndpoint(new Uri("queue:q_error")));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().First(e => e.Queue.Name == "q");

        // assert
        Assert.Equal(
            "q_error",
            ((RabbitMQQueue)endpoint.Features.Get<ReceiveFaultEndpointFeature>()!.Endpoint!.Destination).Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetSkippedEndpointDestinationName_When_SkippedEndpointConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
            t.Queue("q").AutoProvision().Handler<OrderCreatedHandler>().SkippedEndpoint(new Uri("queue:q_skipped")));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().First(e => e.Queue.Name == "q");

        // assert
        Assert.Equal(
            "q_skipped",
            ((RabbitMQQueue)endpoint.Features.Get<ReceiveSkippedEndpointFeature>()!.Endpoint!.Destination).Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetKind_When_KindConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
            t.Queue("err-q").AutoProvision().Handler<OrderCreatedHandler>().Kind(ReceiveEndpointKind.Error));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().First(e => e.Queue.Name == "err-q");

        // assert
        Assert.Equal(ReceiveEndpointKind.Error, endpoint.Kind);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetSourceAddress_When_QueueConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
            t.Queue("my-q").AutoProvision().Handler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().First(e => e.Queue.Name == "my-q");

        // assert
        Assert.NotNull(endpoint.Source.Address);
        Assert.Contains("q/my-q", endpoint.Source.Address.ToString());
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetAddress_When_Completed()
    {
        // arrange
        var runtime = CreateRuntime(t =>
            t.Queue("my-q").AutoProvision().Handler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().First(e => e.Queue.Name == "my-q");

        // assert
        Assert.NotNull(endpoint.Address);
        Assert.Equal("rabbitmq", endpoint.Address.Scheme);
    }

    private static MessagingRuntime CreateRuntime(Action<IRabbitMQMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configure(t);
            })
            .BuildRuntime();
        return runtime;
    }
}
