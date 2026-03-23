using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Inbox;
using Mocha.Middlewares;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class InboxTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Inbox_Should_DeduplicateMessage_When_SameMessageIdPublishedTwice()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();
        var messageId = Guid.NewGuid().ToString();

        await using var provider = await CreateBusWithInboxAsync(
            inbox,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<InboxEventHandler>();

                // Force every dispatched message to use the same MessageId
                b.ConfigureMessageBus(h =>
                    h.UseDispatch(new DispatchMiddlewareConfiguration(
                        (_, next) =>
                            ctx =>
                            {
                                ctx.MessageId = messageId;
                                return next(ctx);
                            },
                        "ForceMessageId"),
                        before: "Instrumentation"));
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish the first message; handler should process it
        await bus.PublishAsync(new InboxEvent { Payload = "first" }, CancellationToken.None);
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the first event");
        await WaitUntilAsync(() => inbox.RecordedEnvelopes.Count >= 1, s_timeout);

        // act - publish a second message with the same MessageId; handler should NOT process it
        await bus.PublishAsync(new InboxEvent { Payload = "second" }, CancellationToken.None);

        // assert - only the first message was handled
        Assert.False(
            await recorder.WaitAsync(TimeSpan.FromMilliseconds(500), expectedCount: 2),
            "Handler should not have received the duplicate message");
        Assert.Single(recorder.Messages);

        var handled = Assert.IsType<InboxEvent>(recorder.Messages.First());
        Assert.Equal("first", handled.Payload);
    }

    [Fact]
    public async Task Inbox_Should_ProcessBothMessages_When_DifferentMessageIds()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();

        await using var provider = await CreateBusWithInboxAsync(
            inbox,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<InboxEventHandler>();
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish two distinct messages (each gets a unique auto-generated MessageId)
        await bus.PublishAsync(new InboxEvent { Payload = "alpha" }, CancellationToken.None);
        await bus.PublishAsync(new InboxEvent { Payload = "beta" }, CancellationToken.None);

        // assert - both messages are handled
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 2),
            "Handler did not receive both events within timeout");

        Assert.Equal(2, recorder.Messages.Count);

        var payloads = recorder.Messages
            .Cast<InboxEvent>()
            .Select(e => e.Payload)
            .OrderBy(p => p)
            .ToList();

        Assert.Equal(["alpha", "beta"], payloads);

        // Both should be recorded in the inbox
        await WaitUntilAsync(() => inbox.RecordedEnvelopes.Count >= 2, s_timeout);
        Assert.Equal(2, inbox.RecordedEnvelopes.Count);
    }

    [Fact]
    public async Task Inbox_Should_ProcessMessage_When_SkipInboxIsSet()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();
        var messageId = Guid.NewGuid().ToString();

        // Pre-seed the inbox so the MessageId is already "processed"
        inbox.Seed(messageId);

        await using var provider = await CreateBusWithInboxAsync(
            inbox,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<InboxEventHandler>();

                // Force the dispatched message to use the pre-seeded MessageId
                b.ConfigureMessageBus(h =>
                    h.UseDispatch(new DispatchMiddlewareConfiguration(
                        (_, next) =>
                            ctx =>
                            {
                                ctx.MessageId = messageId;
                                return next(ctx);
                            },
                        "ForceMessageId")));

                // Add a consumer middleware before inbox that sets SkipInbox
                b.ConfigureMessageBus(h =>
                    h.UseConsume(
                        new ConsumerMiddlewareConfiguration(
                            static (_, next) =>
                                ctx =>
                                {
                                    var feature = ctx.Features.GetOrSet<InboxMiddlewareFeature>();
                                    feature.SkipInbox = true;
                                    return next(ctx);
                                },
                            "SkipInboxCheck"),
                        before: "Inbox"));
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - even though the MessageId is already in the inbox, SkipInbox bypasses the check
        await bus.PublishAsync(new InboxEvent { Payload = "skip-inbox" }, CancellationToken.None);

        // assert - handler received the message despite the duplicate MessageId
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event within timeout");

        var handled = Assert.IsType<InboxEvent>(Assert.Single(recorder.Messages));
        Assert.Equal("skip-inbox", handled.Payload);
    }

    [Fact]
    public async Task Inbox_Should_ProcessMessage_When_MessageIdIsNull()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();

        await using var provider = await CreateBusWithInboxAsync(
            inbox,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<InboxEventHandler>();

                // Null out the MessageId on the dispatch side
                b.ConfigureMessageBus(h =>
                    h.UseDispatch(new DispatchMiddlewareConfiguration(
                        (_, next) =>
                            ctx =>
                            {
                                ctx.MessageId = null;
                                return next(ctx);
                            },
                        "NullifyMessageId"),
                        before: "Instrumentation"));
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish two messages with null MessageIds
        await bus.PublishAsync(new InboxEvent { Payload = "no-id-1" }, CancellationToken.None);
        await bus.PublishAsync(new InboxEvent { Payload = "no-id-2" }, CancellationToken.None);

        // assert - both are processed (null MessageId means no dedup)
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 2),
            "Handler did not receive both events within timeout");

        Assert.Equal(2, recorder.Messages.Count);
    }

    [Fact]
    public async Task Inbox_Should_SkipSucceededHandler_And_RetryFailedHandler_When_MultipleHandlersRegistered()
    {
        // arrange
        var inbox = new TransactionalInMemoryMessageInbox();
        var succeedingCounter = new InvocationCounter();
        var failingCounter = new InvocationCounter();
        var messageId = Guid.NewGuid().ToString();

        await using var provider = await CreateBusWithTransactionalInboxAsync(
            inbox,
            b =>
            {
                b.Services.AddKeyedSingleton("succeeding", succeedingCounter);
                b.Services.AddKeyedSingleton("failing", failingCounter);
                b.Services.AddSingleton<FailingHandlerAttemptTracker>();
                b.AddEventHandler<SucceedingHandler>();
                b.AddEventHandler<FailingHandler>();

                // Force every dispatched message to use the same MessageId
                b.ConfigureMessageBus(h =>
                    h.UseDispatch(new DispatchMiddlewareConfiguration(
                        (_, next) =>
                            ctx =>
                            {
                                ctx.MessageId = messageId;
                                return next(ctx);
                            },
                        "ForceMessageId"),
                        before: "Instrumentation"));
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - first publish: FailingHandler will throw on first attempt.
        // The consumer loop in DefaultPipeline iterates handlers sequentially;
        // when FailingHandler throws, the exception propagates and any handler
        // not yet reached in that iteration does not run for this delivery.
        // The transactional rollback middleware removes the failed handler's claim.
        await bus.PublishAsync(new MultiHandlerEvent { Payload = "attempt-1" }, CancellationToken.None);

        // Wait for the failing handler to have been invoked (it throws on first attempt)
        Assert.True(
            await failingCounter.WaitForCountAsync(1, s_timeout),
            "FailingHandler was not invoked on the first attempt");

        // Give the first delivery time to fully settle (fault middleware, etc.)
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        // Capture counters after first delivery.
        // Depending on consumer iteration order:
        // - If SucceedingHandler ran first: succeedingCount=1, failingCount=1
        // - If FailingHandler ran first: succeedingCount=0, failingCount=1
        var succeedingCountAfterFirstDelivery = succeedingCounter.Count;
        var failingCountAfterFirstDelivery = failingCounter.Count;

        Assert.Equal(1, failingCountAfterFirstDelivery);
        Assert.True(
            succeedingCountAfterFirstDelivery is 0 or 1,
            $"SucceedingHandler should have been invoked 0 or 1 times, was {succeedingCountAfterFirstDelivery}");

        // act - re-publish with same MessageId to simulate transport redelivery.
        // On this delivery:
        // - SucceedingHandler: if it ran before, inbox dedup skips it; if it didn't run, it processes now
        // - FailingHandler: claim was rolled back, so inbox allows retry; second attempt succeeds
        await bus.PublishAsync(new MultiHandlerEvent { Payload = "attempt-2" }, CancellationToken.None);

        // Wait for the failing handler to succeed on retry (second invocation)
        Assert.True(
            await failingCounter.WaitForCountAsync(2, s_timeout),
            "FailingHandler was not retried on the second delivery");

        // If SucceedingHandler didn't run on first delivery, it should run now
        if (succeedingCountAfterFirstDelivery == 0)
        {
            Assert.True(
                await succeedingCounter.WaitForCountAsync(1, s_timeout),
                "SucceedingHandler should have processed on the second delivery");
        }

        // Give a short window to ensure no extra invocation arrives
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        // assert - across both deliveries:
        // SucceedingHandler invoked exactly once (either on first or second delivery, but not both)
        Assert.Equal(1, succeedingCounter.Count);

        // FailingHandler invoked exactly twice (failed first attempt + succeeded retry)
        Assert.Equal(2, failingCounter.Count);

        // Both handlers should now have committed claims in the inbox
        Assert.True(
            await inbox.ExistsAsync(messageId, typeof(SucceedingHandler).FullName!, CancellationToken.None),
            "SucceedingHandler should have a committed inbox claim");
        Assert.True(
            await inbox.ExistsAsync(messageId, typeof(FailingHandler).FullName!, CancellationToken.None),
            "FailingHandler should have a committed inbox claim after successful retry");
    }

    // ══════════════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════════════

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!condition())
        {
            await Task.Delay(50, cts.Token);
        }
    }

    private static async Task<ServiceProvider> CreateBusWithInboxAsync(
        InMemoryMessageInbox inbox,
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageInbox>(inbox);

        var builder = services.AddMessageBus();
        builder.UseInboxCore();

        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    private static async Task<ServiceProvider> CreateBusWithTransactionalInboxAsync(
        TransactionalInMemoryMessageInbox inbox,
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageInbox>(inbox);

        var builder = services.AddMessageBus();
        builder.UseInboxCore();

        // Add a consumer middleware BEFORE the inbox that rolls back the claim on failure.
        // This simulates the transactional behavior where a DB transaction rollback would
        // remove the inbox claim when the handler throws.
        // The inbox is captured directly in the closure (not resolved from DI) because the
        // consumer middleware factory runs against an internal service provider that does not
        // contain application-level singletons.
        builder.ConfigureMessageBus(h =>
            h.UseConsume(
                new ConsumerMiddlewareConfiguration(
                    (_, next) =>
                        async ctx =>
                        {
                            try
                            {
                                await next(ctx);
                            }
                            catch
                            {
                                // On failure, roll back the inbox claim so the message can be
                                // reprocessed by this consumer on redelivery.
                                var msgId = ctx.MessageId;
                                var consumer = ctx.Features.Get<ReceiveConsumerFeature>()?.CurrentConsumer;
                                var consumerType = consumer?.Identity?.FullName ?? "unknown";
                                if (msgId is not null)
                                {
                                    inbox.RemoveClaim(msgId, consumerType);
                                }

                                throw;
                            }
                        },
                    "InboxTransactionRollback"),
                before: "Inbox"));

        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Test types
    // ══════════════════════════════════════════════════════════════════════

    public sealed class InboxEvent
    {
        public required string Payload { get; init; }
    }

    public sealed class InboxEventHandler(MessageRecorder recorder) : IEventHandler<InboxEvent>
    {
        public ValueTask HandleAsync(InboxEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    /// <summary>
    /// In-memory inbox that tracks processed message IDs and recorded envelopes for test assertions.
    /// </summary>
    internal sealed class InMemoryMessageInbox : IMessageInbox
    {
        private readonly ConcurrentDictionary<(string MessageId, string ConsumerType), MessageEnvelope> _processed = new();

        public ConcurrentBag<MessageEnvelope> RecordedEnvelopes { get; } = [];

        /// <summary>
        /// Pre-seeds a message ID into the inbox so it appears as already processed for all consumer types.
        /// Uses a well-known consumer type for seeding.
        /// </summary>
        public void Seed(string messageId)
        {
            _processed.TryAdd((messageId, "*"), new MessageEnvelope { MessageId = messageId });
        }

        public ValueTask<bool> ExistsAsync(string messageId, string consumerType, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(
                _processed.ContainsKey((messageId, consumerType))
                || _processed.ContainsKey((messageId, "*")));
        }

        public ValueTask<bool> TryClaimAsync(MessageEnvelope envelope, string consumerType, CancellationToken cancellationToken)
        {
            if (envelope.MessageId is null)
            {
                return ValueTask.FromResult(false);
            }

            // Check if seeded with wildcard
            if (_processed.ContainsKey((envelope.MessageId, "*")))
            {
                return ValueTask.FromResult(false);
            }

            var claimed = _processed.TryAdd((envelope.MessageId, consumerType), envelope);
            if (claimed)
            {
                RecordedEnvelopes.Add(envelope);
            }

            return ValueTask.FromResult(claimed);
        }

        public ValueTask RecordAsync(MessageEnvelope envelope, string consumerType, CancellationToken cancellationToken)
        {
            if (envelope.MessageId is not null)
            {
                _processed.TryAdd((envelope.MessageId, consumerType), envelope);
            }

            RecordedEnvelopes.Add(envelope);
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> CleanupAsync(TimeSpan maxAge, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(0);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Multi-handler inbox test types
    // ══════════════════════════════════════════════════════════════════════

    public sealed class MultiHandlerEvent
    {
        public required string Payload { get; init; }
    }

    /// <summary>
    /// Thread-safe invocation counter with async wait support.
    /// Registered as a singleton so it persists across scoped handler instances.
    /// </summary>
    public sealed class InvocationCounter
    {
        private int _count;
        private readonly SemaphoreSlim _semaphore = new(0);

        public int Count => Volatile.Read(ref _count);

        public void Increment()
        {
            Interlocked.Increment(ref _count);
            _semaphore.Release();
        }

        public async Task<bool> WaitForCountAsync(int expectedCount, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            try
            {
                while (Volatile.Read(ref _count) < expectedCount)
                {
                    if (!await _semaphore.WaitAsync(timeout))
                    {
                        return Volatile.Read(ref _count) >= expectedCount;
                    }
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                return Volatile.Read(ref _count) >= expectedCount;
            }
        }
    }

    /// <summary>
    /// Tracks how many times the failing handler has been attempted.
    /// Registered as a singleton so it persists across scoped handler instances.
    /// </summary>
    public sealed class FailingHandlerAttemptTracker
    {
        private int _attempts;

        public int IncrementAndGet() => Interlocked.Increment(ref _attempts);
    }

    /// <summary>
    /// Handler that always succeeds and increments a counter.
    /// </summary>
    public sealed class SucceedingHandler(
        [FromKeyedServices("succeeding")] InvocationCounter counter) : IEventHandler<MultiHandlerEvent>
    {
        public ValueTask HandleAsync(MultiHandlerEvent message, CancellationToken cancellationToken)
        {
            counter.Increment();
            return default;
        }
    }

    /// <summary>
    /// Handler that throws on the first invocation and succeeds on subsequent invocations.
    /// The attempt counter is tracked via a singleton <see cref="FailingHandlerAttemptTracker"/>
    /// because handlers are scoped and a new instance is created for each message delivery.
    /// </summary>
    public sealed class FailingHandler(
        [FromKeyedServices("failing")] InvocationCounter counter,
        FailingHandlerAttemptTracker attemptTracker) : IEventHandler<MultiHandlerEvent>
    {
        public ValueTask HandleAsync(MultiHandlerEvent message, CancellationToken cancellationToken)
        {
            counter.Increment();

            if (attemptTracker.IncrementAndGet() == 1)
            {
                throw new InvalidOperationException("Deliberate failure on first attempt");
            }

            return default;
        }
    }

    /// <summary>
    /// In-memory inbox that supports claim removal, simulating transaction rollback behavior.
    /// When a handler fails within a transaction, the claim INSERT is rolled back, allowing
    /// the message to be reprocessed by that handler on redelivery.
    /// </summary>
    internal sealed class TransactionalInMemoryMessageInbox : IMessageInbox
    {
        private readonly ConcurrentDictionary<(string MessageId, string ConsumerType), MessageEnvelope> _processed = new();

        public ConcurrentBag<MessageEnvelope> RecordedEnvelopes { get; } = [];

        /// <summary>
        /// Removes a previously claimed entry, simulating a transaction rollback.
        /// </summary>
        public void RemoveClaim(string messageId, string consumerType)
        {
            _processed.TryRemove((messageId, consumerType), out _);
        }

        public ValueTask<bool> ExistsAsync(string messageId, string consumerType, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_processed.ContainsKey((messageId, consumerType)));
        }

        public ValueTask<bool> TryClaimAsync(MessageEnvelope envelope, string consumerType, CancellationToken cancellationToken)
        {
            if (envelope.MessageId is null)
            {
                return ValueTask.FromResult(false);
            }

            var claimed = _processed.TryAdd((envelope.MessageId, consumerType), envelope);
            if (claimed)
            {
                RecordedEnvelopes.Add(envelope);
            }

            return ValueTask.FromResult(claimed);
        }

        public ValueTask RecordAsync(MessageEnvelope envelope, string consumerType, CancellationToken cancellationToken)
        {
            if (envelope.MessageId is not null)
            {
                _processed.TryAdd((envelope.MessageId, consumerType), envelope);
            }

            RecordedEnvelopes.Add(envelope);
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> CleanupAsync(TimeSpan maxAge, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(0);
        }
    }
}
