using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Descriptors;

public class RabbitMQQueueFrontDoorTests
{
    [Fact]
    public void Queue_Should_ResolveSameEndpoint_When_EndpointSharesQueueName()
    {
        // arrange
        // Calling t.Queue("orders") twice should return the same adapter and produce exactly
        // one receive endpoint, not two separate ones.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                var first = t.Queue("orders");
                first.Consumer<OrderSpyConsumer>();
                // second call with same name must return the same backing endpoint
                var second = t.Queue("orders");
                second.AutoBind(false);
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert: exactly one receive endpoint with queue "orders"
        Assert.Single(
            transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>(),
            e => e.Queue.Name == "orders");
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Queue_Should_MergeOntoRenamedEndpoint_When_EndpointQueueNameMatches()
    {
        // arrange
        // Endpoint("ep").Queue("custom-q") creates an endpoint named "ep" whose queue is "custom-q".
        // A subsequent t.Queue("custom-q") must find that endpoint by queue name and merge onto it,
        // not create a second receive endpoint.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("ep").Queue("custom-q").Consumer<OrderSpyConsumer>();
                // merge path: Queue("custom-q") locates the endpoint above by queue name
                t.Queue("custom-q").AutoBind(false);
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert: still exactly one receive endpoint (no duplicate was created)
        Assert.Single(
            transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>(),
            e => e.Queue.Name == "custom-q");
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configureTransport(t);
            })
            .BuildRuntime();
        return runtime;
    }

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
