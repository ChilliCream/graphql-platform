using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.RabbitMQ.Middlewares;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

public class RabbitMQRoutingKeyTests
{
    [Fact]
    public void UseRabbitMQRoutingKey_Should_StoreExtractorOnMessageType()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseRabbitMQRoutingKey<OrderCreated>(msg => msg.OrderId)));

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.True(messageType!.Features.TryGet<RabbitMQRoutingKeyExtractor>(out var extractor));

        var order = new OrderCreated { OrderId = "ORD-123" };
        Assert.Equal("ORD-123", extractor.Extract(order));
    }

    [Fact]
    public void UseRabbitMQRoutingKey_Should_ExtractCorrectValue_When_CompositeKey()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseRabbitMQRoutingKey<OrderCreated>(msg => $"{msg.OrderId}.priority")));

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.True(messageType!.Features.TryGet<RabbitMQRoutingKeyExtractor>(out var extractor));

        var order = new OrderCreated { OrderId = "ORD-456" };
        Assert.Equal("ORD-456.priority", extractor.Extract(order));
    }

    [Fact]
    public void UseRabbitMQRoutingKey_Should_ReturnNull_When_ExtractorReturnsNull()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseRabbitMQRoutingKey<OrderCreated>(_ => null)));

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.True(messageType!.Features.TryGet<RabbitMQRoutingKeyExtractor>(out var extractor));

        var order = new OrderCreated { OrderId = "ORD-789" };
        Assert.Null(extractor.Extract(order));
    }

    [Fact]
    public void UseRabbitMQRoutingKey_Should_NotStoreExtractor_When_NotConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.Publish(r => r.ToRabbitMQExchange("orders"))));

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.False(messageType!.Features.TryGet<RabbitMQRoutingKeyExtractor>(out _));
    }

    [Fact]
    public async Task Middleware_Should_SetRoutingKeyHeader_When_ExtractorConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseRabbitMQRoutingKey<OrderCreated>(msg => msg.OrderId)));

        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);

        var context = new DispatchContext();
        context.MessageType = messageType;
        context.Message = new OrderCreated { OrderId = "ORD-999" };

        string? capturedRoutingKey = null;
        DispatchDelegate terminal = ctx =>
        {
            ctx.Headers.TryGetValue("x-routing-key", out var value);
            capturedRoutingKey = value as string;
            return default;
        };

        var middleware = new RabbitMQRoutingKeyMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.Equal("ORD-999", capturedRoutingKey);
    }

    [Fact]
    public async Task Middleware_Should_NotSetRoutingKeyHeader_When_ExtractorNotConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.Publish(r => r.ToRabbitMQExchange("orders"))));

        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);

        var context = new DispatchContext();
        context.MessageType = messageType;
        context.Message = new OrderCreated { OrderId = "ORD-000" };

        var nextCalled = false;
        DispatchDelegate terminal = _ =>
        {
            nextCalled = true;
            return default;
        };

        var middleware = new RabbitMQRoutingKeyMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.True(nextCalled);
        Assert.False(context.Headers.TryGetValue("x-routing-key", out _));
    }

    [Fact]
    public async Task Middleware_Should_NotSetRoutingKeyHeader_When_ExtractorReturnsNull()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseRabbitMQRoutingKey<OrderCreated>(_ => null)));

        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);

        var context = new DispatchContext();
        context.MessageType = messageType;
        context.Message = new OrderCreated { OrderId = "ORD-111" };

        var nextCalled = false;
        DispatchDelegate terminal = _ =>
        {
            nextCalled = true;
            return default;
        };

        var middleware = new RabbitMQRoutingKeyMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.True(nextCalled);
        Assert.False(context.Headers.TryGetValue("x-routing-key", out _));
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
