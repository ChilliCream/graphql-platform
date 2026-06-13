using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Routing;

public class RabbitMQDestinationResolverTests
{
    [Fact]
    public void ResolveDestination_Should_UseConventionExchange_When_DestinationNotConfigured()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddMessage<OrderCreated>(d => d.Publish(_ => { })));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();
        var resolver = new RabbitMQDestinationResolver(RabbitMQTransportConfiguration.DefaultSchema);
        var expectedName = runtime.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act
        var resolution = resolver.ResolveDestination(runtime.Naming, route);

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
        var resolver = new RabbitMQDestinationResolver(RabbitMQTransportConfiguration.DefaultSchema);

        // act
        var resolution = resolver.ResolveDestination(runtime.Naming, route);

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
        var resolver = new RabbitMQDestinationResolver(RabbitMQTransportConfiguration.DefaultSchema);

        // act
        var resolution = resolver.ResolveDestination(runtime.Naming, route);

        // assert
        Assert.Equal(RabbitMQDestinationKind.Queue, resolution.Kind);
        Assert.Equal("orders-queue", resolution.Name);
        Assert.Equal("q/orders-queue", resolution.EndpointName);
    }

    [Fact]
    public void ResolveBindKey_Should_ReturnNone_When_NoRoutingKeyConfigured()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddMessage<OrderCreated>(d => d.Publish(_ => { })));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var resolver = new RabbitMQDestinationResolver(RabbitMQTransportConfiguration.DefaultSchema);

        // act
        var resolution = resolver.ResolveBindKey(messageType);

        // assert
        Assert.Equal(RabbitMQBindKeyKind.None, resolution.Kind);
        Assert.Null(resolution.Key);
    }

    [Fact]
    public void ResolveBindKey_Should_ReturnUnderivable_When_CustomRoutingKeyFunctionConfigured()
    {
        // arrange
        // a per-message routing-key function cannot be evaluated at configuration time, so a consume
        // binding for the type cannot be derived without guessing a key.
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d => d.UseRabbitMQRoutingKey<OrderCreated>(m => m.OrderId)));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var resolver = new RabbitMQDestinationResolver(RabbitMQTransportConfiguration.DefaultSchema);

        // act
        var resolution = resolver.ResolveBindKey(messageType);

        // assert
        Assert.Equal(RabbitMQBindKeyKind.Underivable, resolution.Kind);
        Assert.Null(resolution.Key);
    }

    [Fact]
    public void ConsumeBindUnderivable_Should_IncludeMessageTypeName()
    {
        // arrange
        var messageTypeName = typeof(OrderCreated).FullName!;
        const string queueName = "test-queue";

        // act
        var ex = ThrowHelper.ConsumeBindUnderivable(messageTypeName, queueName);

        // assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains(messageTypeName, ex.Message);
    }

    [Fact]
    public void ConsumeBindUnderivable_Should_IncludeQueueName()
    {
        // arrange
        var messageTypeName = typeof(OrderCreated).FullName!;
        const string queueName = "my-orders-queue";

        // act
        var ex = ThrowHelper.ConsumeBindUnderivable(messageTypeName, queueName);

        // assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains(queueName, ex.Message);
    }

    [Fact]
    public void ConsumeBindUnderivable_Should_IncludeRemediationInstructions()
    {
        // arrange
        var messageTypeName = typeof(OrderCreated).FullName!;
        const string queueName = "test-queue";

        // act
        var ex = ThrowHelper.ConsumeBindUnderivable(messageTypeName, queueName);

        // assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("static routing key", ex.Message);
        Assert.Contains("BindFrom", ex.Message);
        Assert.Contains("Receives", ex.Message);
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
