using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.AzureServiceBus.Middlewares;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests;

public class AzureServiceBusMessagePropertiesTests
{
    private const string FakeConnectionString =
        "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=a2V5";

    [Fact]
    public void UseAzureServiceBusSessionId_Should_StoreExtractorOnMessageType()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusSessionId<OrderCreated>(msg => msg.OrderId)));

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.True(messageType!.Features.TryGet<AzureServiceBusSessionIdExtractor>(out var extractor));

        var order = new OrderCreated { OrderId = "ORD-123" };
        Assert.Equal("ORD-123", extractor.Extract(order));
    }

    [Fact]
    public void UseAzureServiceBusPartitionKey_Should_StoreExtractorOnMessageType()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusPartitionKey<OrderCreated>(msg => msg.OrderId)));

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.True(messageType!.Features.TryGet<AzureServiceBusPartitionKeyExtractor>(out var extractor));

        var order = new OrderCreated { OrderId = "ORD-456" };
        Assert.Equal("ORD-456", extractor.Extract(order));
    }

    [Fact]
    public void UseAzureServiceBusReplyToSessionId_Should_StoreExtractorOnMessageType()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusReplyToSessionId<OrderCreated>(msg => $"reply-{msg.OrderId}")));

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.True(messageType!.Features.TryGet<AzureServiceBusReplyToSessionIdExtractor>(out var extractor));

        var order = new OrderCreated { OrderId = "ORD-789" };
        Assert.Equal("reply-ORD-789", extractor.Extract(order));
    }

    [Fact]
    public void UseAzureServiceBusTo_Should_StoreExtractorOnMessageType()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusTo<OrderCreated>(msg => $"forward://{msg.OrderId}")));

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.True(messageType!.Features.TryGet<AzureServiceBusToExtractor>(out var extractor));

        var order = new OrderCreated { OrderId = "ORD-321" };
        Assert.Equal("forward://ORD-321", extractor.Extract(order));
    }

    [Fact]
    public void Extractor_Should_ReturnNull_When_ExtractorReturnsNull()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusSessionId<OrderCreated>(_ => null)));

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.True(messageType!.Features.TryGet<AzureServiceBusSessionIdExtractor>(out var extractor));

        var order = new OrderCreated { OrderId = "ORD-000" };
        Assert.Null(extractor.Extract(order));
    }

    [Fact]
    public async Task Middleware_Should_SetSessionIdHeader_When_ExtractorConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusSessionId<OrderCreated>(msg => msg.OrderId)));

        var context = new DispatchContext
        {
            MessageType = runtime.Messages.GetMessageType(typeof(OrderCreated)),
            Message = new OrderCreated { OrderId = "ORD-111" }
        };

        string? captured = null;
        DispatchDelegate terminal = ctx =>
        {
            ctx.Headers.TryGetValue(AzureServiceBusMessageHeaders.SessionId, out var value);
            captured = value as string;
            return default;
        };

        var middleware = new AzureServiceBusMessagePropertiesMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.Equal("ORD-111", captured);
    }

    [Fact]
    public async Task Middleware_Should_SetPartitionKeyHeader_When_ExtractorConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusPartitionKey<OrderCreated>(msg => msg.OrderId)));

        var context = new DispatchContext
        {
            MessageType = runtime.Messages.GetMessageType(typeof(OrderCreated)),
            Message = new OrderCreated { OrderId = "ORD-222" }
        };

        string? captured = null;
        DispatchDelegate terminal = ctx =>
        {
            ctx.Headers.TryGetValue(AzureServiceBusMessageHeaders.PartitionKey, out var value);
            captured = value as string;
            return default;
        };

        var middleware = new AzureServiceBusMessagePropertiesMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.Equal("ORD-222", captured);
    }

    [Fact]
    public async Task Middleware_Should_SetReplyToSessionIdHeader_When_ExtractorConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusReplyToSessionId<OrderCreated>(msg => $"reply-{msg.OrderId}")));

        var context = new DispatchContext
        {
            MessageType = runtime.Messages.GetMessageType(typeof(OrderCreated)),
            Message = new OrderCreated { OrderId = "ORD-333" }
        };

        string? captured = null;
        DispatchDelegate terminal = ctx =>
        {
            ctx.Headers.TryGetValue(AzureServiceBusMessageHeaders.ReplyToSessionId, out var value);
            captured = value as string;
            return default;
        };

        var middleware = new AzureServiceBusMessagePropertiesMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.Equal("reply-ORD-333", captured);
    }

    [Fact]
    public async Task Middleware_Should_SetToHeader_When_ExtractorConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusTo<OrderCreated>(msg => $"forward://{msg.OrderId}")));

        var context = new DispatchContext
        {
            MessageType = runtime.Messages.GetMessageType(typeof(OrderCreated)),
            Message = new OrderCreated { OrderId = "ORD-444" }
        };

        string? captured = null;
        DispatchDelegate terminal = ctx =>
        {
            ctx.Headers.TryGetValue(AzureServiceBusMessageHeaders.To, out var value);
            captured = value as string;
            return default;
        };

        var middleware = new AzureServiceBusMessagePropertiesMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.Equal("forward://ORD-444", captured);
    }

    [Fact]
    public async Task Middleware_Should_NotSetHeaders_When_ExtractorsNotConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d => d.Send(r => r.ToQueue("orders"))));

        var context = new DispatchContext
        {
            MessageType = runtime.Messages.GetMessageType(typeof(OrderCreated)),
            Message = new OrderCreated { OrderId = "ORD-000" }
        };

        var nextCalled = false;
        DispatchDelegate terminal = _ =>
        {
            nextCalled = true;
            return default;
        };

        var middleware = new AzureServiceBusMessagePropertiesMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.True(nextCalled);
        Assert.False(context.Headers.TryGetValue(AzureServiceBusMessageHeaders.SessionId, out _));
        Assert.False(context.Headers.TryGetValue(AzureServiceBusMessageHeaders.PartitionKey, out _));
        Assert.False(context.Headers.TryGetValue(AzureServiceBusMessageHeaders.ReplyToSessionId, out _));
        Assert.False(context.Headers.TryGetValue(AzureServiceBusMessageHeaders.To, out _));
    }

    [Fact]
    public async Task Middleware_Should_NotSetHeader_When_ExtractorReturnsNull()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusSessionId<OrderCreated>(_ => null)));

        var context = new DispatchContext
        {
            MessageType = runtime.Messages.GetMessageType(typeof(OrderCreated)),
            Message = new OrderCreated { OrderId = "ORD-555" }
        };

        DispatchDelegate terminal = _ => default;

        var middleware = new AzureServiceBusMessagePropertiesMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.False(context.Headers.TryGetValue(AzureServiceBusMessageHeaders.SessionId, out _));
    }

    [Fact]
    public async Task Middleware_Should_NotInvokeExtractor_When_SessionIdHeaderAlreadySet()
    {
        // arrange
        var extractorCallCount = 0;
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusSessionId<OrderCreated>(msg =>
                {
                    extractorCallCount++;
                    return msg.OrderId;
                })));

        var context = new DispatchContext
        {
            MessageType = runtime.Messages.GetMessageType(typeof(OrderCreated)),
            Message = new OrderCreated { OrderId = "ORD-EXTR" }
        };

        context.Headers.Set(AzureServiceBusMessageHeaders.SessionId, "user-supplied");

        DispatchDelegate terminal = _ => default;

        var middleware = new AzureServiceBusMessagePropertiesMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.Equal(0, extractorCallCount);
        Assert.True(context.Headers.TryGetValue(AzureServiceBusMessageHeaders.SessionId, out var value));
        Assert.Equal("user-supplied", value);
    }

    [Fact]
    public async Task Middleware_Should_NotInvokeExtractor_When_PartitionKeyHeaderAlreadySet()
    {
        // arrange
        var extractorCallCount = 0;
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusPartitionKey<OrderCreated>(msg =>
                {
                    extractorCallCount++;
                    return msg.OrderId;
                })));

        var context = new DispatchContext
        {
            MessageType = runtime.Messages.GetMessageType(typeof(OrderCreated)),
            Message = new OrderCreated { OrderId = "ORD-EXTR" }
        };

        context.Headers.Set(AzureServiceBusMessageHeaders.PartitionKey, "user-supplied");

        DispatchDelegate terminal = _ => default;

        var middleware = new AzureServiceBusMessagePropertiesMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.Equal(0, extractorCallCount);
        Assert.True(context.Headers.TryGetValue(AzureServiceBusMessageHeaders.PartitionKey, out var value));
        Assert.Equal("user-supplied", value);
    }

    [Fact]
    public async Task Middleware_Should_NotInvokeExtractor_When_ReplyToSessionIdHeaderAlreadySet()
    {
        // arrange
        var extractorCallCount = 0;
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusReplyToSessionId<OrderCreated>(msg =>
                {
                    extractorCallCount++;
                    return msg.OrderId;
                })));

        var context = new DispatchContext
        {
            MessageType = runtime.Messages.GetMessageType(typeof(OrderCreated)),
            Message = new OrderCreated { OrderId = "ORD-EXTR" }
        };

        context.Headers.Set(AzureServiceBusMessageHeaders.ReplyToSessionId, "user-supplied");

        DispatchDelegate terminal = _ => default;

        var middleware = new AzureServiceBusMessagePropertiesMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.Equal(0, extractorCallCount);
        Assert.True(context.Headers.TryGetValue(AzureServiceBusMessageHeaders.ReplyToSessionId, out var value));
        Assert.Equal("user-supplied", value);
    }

    [Fact]
    public async Task Middleware_Should_NotInvokeExtractor_When_ToHeaderAlreadySet()
    {
        // arrange
        var extractorCallCount = 0;
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(
                d => d.UseAzureServiceBusTo<OrderCreated>(msg =>
                {
                    extractorCallCount++;
                    return msg.OrderId;
                })));

        var context = new DispatchContext
        {
            MessageType = runtime.Messages.GetMessageType(typeof(OrderCreated)),
            Message = new OrderCreated { OrderId = "ORD-EXTR" }
        };

        context.Headers.Set(AzureServiceBusMessageHeaders.To, "user-supplied");

        DispatchDelegate terminal = _ => default;

        var middleware = new AzureServiceBusMessagePropertiesMiddleware();

        // act
        await middleware.InvokeAsync(context, terminal);

        // assert
        Assert.Equal(0, extractorCallCount);
        Assert.True(context.Headers.TryGetValue(AzureServiceBusMessageHeaders.To, out var value));
        Assert.Equal("user-supplied", value);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        return builder
            .AddAzureServiceBus(FakeConnectionString)
            .BuildRuntime();
    }
}
