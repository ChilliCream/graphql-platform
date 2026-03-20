using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Features;
using Mocha.Inbox;
using Mocha.Middlewares;

namespace Mocha.Tests.Inbox;

public class ConsumeInboxMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_MessageIdIsNull()
    {
        // Arrange
        var inbox = new InMemoryMessageInbox();
        var nextCalled = false;

        var middleware = new ConsumeInboxMiddleware(NullLogger<ConsumeInboxMiddleware>.Instance);
        var context = CreateConsumeContext(inbox, messageId: null);

        ConsumerDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        Assert.True(nextCalled, "Next delegate should be called when MessageId is null");
        Assert.Empty(inbox.RecordedEnvelopes);
    }

    [Fact]
    public async Task InvokeAsync_Should_SkipNext_When_MessageAlreadyExists()
    {
        // Arrange
        var inbox = new InMemoryMessageInbox();
        var messageId = Guid.NewGuid().ToString();
        var envelope = new MessageEnvelope { MessageId = messageId, MessageType = "urn:message:test" };

        // Pre-record the message for the same consumer type
        await inbox.RecordAsync(envelope, s_testConsumerType, CancellationToken.None);
        inbox.RecordedEnvelopes.Clear(); // Clear so we can detect if re-recorded

        var nextCalled = false;
        var middleware = new ConsumeInboxMiddleware(NullLogger<ConsumeInboxMiddleware>.Instance);
        var context = CreateConsumeContext(inbox, messageId);
        context.Envelope = envelope;

        ConsumerDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        Assert.False(nextCalled, "Next delegate should NOT be called for duplicate messages");
        Assert.Empty(inbox.RecordedEnvelopes);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNextAndRecord_When_NewMessage()
    {
        // Arrange
        var inbox = new InMemoryMessageInbox();
        var messageId = Guid.NewGuid().ToString();
        var envelope = new MessageEnvelope { MessageId = messageId, MessageType = "urn:message:test" };

        var nextCalled = false;
        var middleware = new ConsumeInboxMiddleware(NullLogger<ConsumeInboxMiddleware>.Instance);
        var context = CreateConsumeContext(inbox, messageId);
        context.Envelope = envelope;

        ConsumerDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        Assert.True(nextCalled, "Next delegate should be called for new messages");
        Assert.Single(inbox.RecordedEnvelopes);
        Assert.Equal(messageId, inbox.RecordedEnvelopes.First().MessageId);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNextWithoutRecording_When_SkipInboxIsTrue()
    {
        // Arrange
        var inbox = new InMemoryMessageInbox();
        var messageId = Guid.NewGuid().ToString();
        var envelope = new MessageEnvelope { MessageId = messageId, MessageType = "urn:message:test" };

        var nextCalled = false;
        var middleware = new ConsumeInboxMiddleware(NullLogger<ConsumeInboxMiddleware>.Instance);
        var context = CreateConsumeContext(inbox, messageId);
        context.Envelope = envelope;

        // Set SkipInbox before middleware runs
        var feature = context.Features.GetOrSet<InboxMiddlewareFeature>();
        feature.SkipInbox = true;

        ConsumerDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        Assert.True(nextCalled, "Next delegate should be called when SkipInbox is true");
        Assert.Empty(inbox.RecordedEnvelopes);
    }

    [Fact]
    public async Task InvokeAsync_Should_NotRecord_When_EnvelopeIsNullAfterNext()
    {
        // Arrange
        var inbox = new InMemoryMessageInbox();
        var messageId = Guid.NewGuid().ToString();

        var middleware = new ConsumeInboxMiddleware(NullLogger<ConsumeInboxMiddleware>.Instance);
        var context = CreateConsumeContext(inbox, messageId);
        // Envelope is intentionally null

        ConsumerDelegate next = _ => ValueTask.CompletedTask;

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert - next was called but no recording because Envelope is null
        Assert.Empty(inbox.RecordedEnvelopes);
    }

    [Fact]
    public async Task InvokeAsync_Should_ProcessOnlyOnce_When_ConcurrentConsumersClaimSameMessage()
    {
        // Arrange
        var inbox = new InMemoryMessageInbox();
        var messageId = Guid.NewGuid().ToString();
        var envelope = new MessageEnvelope { MessageId = messageId, MessageType = "urn:message:test" };

        var processedCount = 0;
        const int concurrency = 50;
        using var barrier = new Barrier(concurrency);

        ConsumerDelegate next = _ =>
        {
            Interlocked.Increment(ref processedCount);
            return ValueTask.CompletedTask;
        };

        // Act - launch N concurrent consumers all trying to process the same MessageId
        var tasks = Enumerable.Range(0, concurrency).Select(_ => Task.Run(async () =>
        {
            var middleware = new ConsumeInboxMiddleware(NullLogger<ConsumeInboxMiddleware>.Instance);
            var context = CreateConsumeContext(inbox, messageId);
            context.Envelope = envelope;

            // Synchronize all tasks to maximize contention
            barrier.SignalAndWait();
            await middleware.InvokeAsync(context, next);
        }));

        await Task.WhenAll(tasks);

        // Assert - exactly one consumer should have processed the message
        Assert.Equal(1, processedCount);
        Assert.Single(inbox.RecordedEnvelopes);
        Assert.Equal(messageId, inbox.RecordedEnvelopes.First().MessageId);
    }

    /// <summary>
    /// The consumer type name used in tests to simulate a consumer identity.
    /// Matches the full type name of the nested <see cref="TestConsumer"/> class.
    /// </summary>
    private static readonly string s_testConsumerType = typeof(TestConsumer).FullName!;

    /// <summary>
    /// Creates a <see cref="ReceiveContext"/> (which implements both
    /// <see cref="IReceiveContext"/> and <see cref="IConsumeContext"/>)
    /// for use in consumer middleware tests.
    /// </summary>
    private static ReceiveContext CreateConsumeContext(
        IMessageInbox inbox,
        string? messageId)
    {
        var services = new ServiceCollection();
        services.AddSingleton(inbox);
        var provider = services.BuildServiceProvider();

        var context = new ReceiveContext();
        context.MessageId = messageId;
        context.Services = provider;

        // Set up the ReceiveConsumerFeature with a mock consumer identity
        // so the inbox middleware can determine the consumer type.
        var consumerFeature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        consumerFeature.CurrentConsumer = new TestConsumer();

        return context;
    }

    /// <summary>
    /// A test consumer whose <see cref="Consumer.Identity"/> type provides the consumer type name
    /// used in inbox deduplication.
    /// </summary>
    private sealed class TestConsumer : Consumer
    {
        public TestConsumer()
        {
            SetIdentity(typeof(TestConsumer));
        }

        protected override ValueTask ConsumeAsync(IConsumeContext context)
            => ValueTask.CompletedTask;
    }
}
