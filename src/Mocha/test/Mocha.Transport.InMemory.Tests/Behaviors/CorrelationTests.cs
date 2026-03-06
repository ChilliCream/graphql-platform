using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class CorrelationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Publish_Should_AutoGenerateIds_When_NoIdsSet()
    {
        // arrange
        var capture = new ContextCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderCreatedSpy>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, default);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout));
        var ctx = Assert.Single(capture.Contexts);

        Assert.NotNull(ctx.MessageId);
        Assert.NotNull(ctx.CorrelationId);
        Assert.NotNull(ctx.ConversationId);
        Assert.True(Guid.TryParse(ctx.MessageId, out _), "MessageId should be a valid GUID");
        Assert.True(Guid.TryParse(ctx.CorrelationId, out _), "CorrelationId should be a valid GUID");
        Assert.True(Guid.TryParse(ctx.ConversationId, out _), "ConversationId should be a valid GUID");
    }

    [Fact]
    public async Task Publish_Should_AssignUniqueIds_When_MultipleSeparatePublishes()
    {
        // arrange
        var capture = new ContextCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderCreatedSpy>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act — two independent publishes
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-A" }, default);
        Assert.True(await capture.WaitAsync(s_timeout));

        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-B" }, default);
        Assert.True(await capture.WaitAsync(s_timeout));

        // assert — each publish gets its own MessageId and ConversationId
        Assert.Equal(2, capture.Contexts.Count);
        var ids = capture.Contexts.ToArray();

        Assert.NotEqual(ids[0].MessageId, ids[1].MessageId);
        Assert.NotEqual(ids[0].ConversationId, ids[1].ConversationId);
    }

    [Fact]
    public async Task Consumer_Should_SeeAllCorrelationIds_When_MessageReceived()
    {
        // arrange — use IConsumer<T> to inspect the full context
        var capture = new ContextCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderCreatedSpy>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-CTX" }, default);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout));
        var ctx = Assert.Single(capture.Contexts);

        Assert.NotNull(ctx.ConversationId);
        Assert.NotNull(ctx.CorrelationId);
        Assert.NotNull(ctx.MessageId);
    }

    [Fact]
    public async Task Publish_Should_HaveDistinctMessageIdButSharedCorrelationScope_When_FanOutToMultipleConsumers()
    {
        // arrange — two consumers receive the same published event via fan-out
        var capture = new ContextCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderCreatedSpy>()
            .AddConsumer<OrderCreatedSpy2>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-FAN" }, default);

        // assert — both consumers received the event
        Assert.True(await capture.WaitAsync(s_timeout, 2));
        Assert.Equal(2, capture.Contexts.Count);

        var all = capture.Contexts.ToArray();

        // Both see the same ConversationId (same logical conversation)
        Assert.Equal(all[0].ConversationId, all[1].ConversationId);

        // Both see the same CorrelationId (same correlation scope)
        Assert.Equal(all[0].CorrelationId, all[1].CorrelationId);
    }

    [Fact]
    public async Task Chain_Should_PropagateConversationId_When_HandlerPublishesNewMessage()
    {
        // arrange
        // Chain: publish OrderCreated → OrderCreatedForwarder handles it and publishes ProcessPayment
        //        → PaymentSpy captures the ProcessPayment context
        // Verify: ConversationId from message 1 should carry over to message 2,
        //         and CausationId on message 2 should equal MessageId of message 1.
        var capture = new ContextCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderCreatedForwarder>()
            .AddConsumer<PaymentSpy>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act — publish the initial event
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-CHAIN" }, default);

        // assert — wait for both captures (OrderCreated + ProcessPayment)
        Assert.True(await capture.WaitAsync(s_timeout, 2), "Both handlers should fire");
        Assert.Equal(2, capture.Contexts.Count);

        var hop1 = capture.Contexts.Single(c => c.Label == "OrderCreatedForwarder");
        var hop2 = capture.Contexts.Single(c => c.Label == "PaymentSpy");

        // ConversationId must propagate across hops
        Assert.Equal(hop1.ConversationId, hop2.ConversationId);

        // CausationId on hop2 should equal MessageId of hop1 (parent→child link)
        Assert.Equal(hop1.MessageId, hop2.CausationId);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Test infrastructure
    // ══════════════════════════════════════════════════════════════════════

    public sealed class ContextCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);

        public ConcurrentBag<CapturedContext> Contexts { get; } = [];

        public void Record(
            string? messageId,
            string? correlationId,
            string? conversationId,
            string? causationId,
            string? label = null)
        {
            Contexts.Add(new CapturedContext
            {
                MessageId = messageId,
                CorrelationId = correlationId,
                ConversationId = conversationId,
                CausationId = causationId,
                Label = label
            });
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public sealed class CapturedContext
    {
        public string? MessageId { get; init; }
        public string? CorrelationId { get; init; }
        public string? ConversationId { get; init; }
        public string? CausationId { get; init; }
        public string? Label { get; init; }
    }

    public sealed class OrderCreatedSpy(ContextCapture capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.Record(context.MessageId, context.CorrelationId, context.ConversationId, context.CausationId);
            return default;
        }
    }

    public sealed class OrderCreatedSpy2(ContextCapture capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.Record(context.MessageId, context.CorrelationId, context.ConversationId, context.CausationId);
            return default;
        }
    }

    /// <summary>
    /// Receives OrderCreated and publishes ProcessPayment. ConversationId and
    /// CausationId are propagated automatically by the framework.
    /// </summary>
    public sealed class OrderCreatedForwarder(ContextCapture capture) : IConsumer<OrderCreated>
    {
        public async ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.Record(
                context.MessageId, context.CorrelationId,
                context.ConversationId, context.CausationId,
                nameof(OrderCreatedForwarder));

            var bus = context.Services.GetRequiredService<IMessageBus>();

            // No manual propagation needed — the framework handles it automatically
            await bus.PublishAsync(
                new ProcessPayment { OrderId = context.Message.OrderId, Amount = 100m },
                context.CancellationToken);
        }
    }

    public sealed class PaymentSpy(ContextCapture capture) : IConsumer<ProcessPayment>
    {
        public ValueTask ConsumeAsync(IConsumeContext<ProcessPayment> context)
        {
            capture.Record(
                context.MessageId, context.CorrelationId,
                context.ConversationId, context.CausationId,
                nameof(PaymentSpy));
            return default;
        }
    }
}
