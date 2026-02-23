using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Topology;

public class RabbitMQMessageTypeExtensionTests
{
    [Fact]
    public void ToRabbitMQQueue_Should_SetDestinationUri_When_DefaultSchema()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddMessage<ProcessPayment>(d => d.Send(r => r.ToRabbitMQQueue("my-queue"))));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Queue is { Name: "my-queue" });
        Assert.Contains("q/my-queue", endpoint.Destination.Address.ToString());
        Assert.StartsWith("rabbitmq:", endpoint.Address.ToString());
    }

    [Fact]
    public void ToRabbitMQQueue_Should_SetDestinationUri_When_CustomSchema()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddMessage<ProcessPayment>(d => d.Send(r => r.ToRabbitMQQueue("custom", "my-queue"))),
            schema: "custom");
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Queue is { Name: "my-queue" });
        Assert.Contains("q/my-queue", endpoint.Destination.Address.ToString());
        Assert.StartsWith("custom:", endpoint.Address.ToString());
    }

    [Fact]
    public void ToRabbitMQExchange_Should_SetDestinationUri_When_DefaultSchema()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddMessage<OrderCreated>(d => d.Publish(r => r.ToRabbitMQExchange("my-exchange"))));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Exchange is { Name: "my-exchange" });
        Assert.Contains("e/my-exchange", endpoint.Destination.Address.ToString());
        Assert.StartsWith("rabbitmq:", endpoint.Address.ToString());
    }

    [Fact]
    public void ToRabbitMQExchange_Should_SetDestinationUri_When_CustomSchema()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d => d.Publish(r => r.ToRabbitMQExchange("custom", "my-exchange"))),
            schema: "custom");
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Exchange is { Name: "my-exchange" });
        Assert.Contains("e/my-exchange", endpoint.Destination.Address.ToString());
        Assert.StartsWith("custom:", endpoint.Address.ToString());
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure, string? schema = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                if (schema is not null)
                {
                    t.Schema(schema);
                }
            })
            .BuildRuntime();
        return runtime;
    }
}
