using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class DispatchEndpointSentAtTests
{
    [Fact]
    public async Task ExecuteAsync_Should_SetSentAtAtTransport_When_MiddlewareAdvancesTime()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var sentAt = timeProvider.GetUtcNow().AddMinutes(5);
        MessageEnvelope? envelopeBeforeDispatch = null;
        DateTimeOffset? contextSentAtBeforeDispatch = sentAt;
        var middleware = new DispatchMiddlewareConfiguration(
            (_, next) => context =>
            {
                envelopeBeforeDispatch = context.Envelope;
                contextSentAtBeforeDispatch = context.SentAt;
                timeProvider.Advance(TimeSpan.FromMinutes(5));
                return next(context);
            },
            "AdvanceTime");
        await using var provider = CreateProvider(timeProvider, middleware);
        var context = new DispatchContext();
        Initialize(context, provider);

        // act
        await context.Endpoint.ExecuteAsync(context);
        using var item = await ReadQueueItemAsync(context);

        // assert
        Assert.Null(envelopeBeforeDispatch);
        Assert.Null(contextSentAtBeforeDispatch);
        Assert.Equal(sentAt, context.SentAt);
        Assert.Equal(sentAt, context.Envelope!.SentAt);
        Assert.Equal(sentAt, item.Envelope.SentAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_Should_PreserveExplicitSentAt_When_SetBeforeInitialization(bool hasEnvelope)
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var sentAt = timeProvider.GetUtcNow().AddDays(-1);
        await using var provider = CreateProvider(timeProvider, null);
        var context = new DispatchContext
        {
            SentAt = sentAt,
            Envelope = hasEnvelope ? new MessageEnvelope { SentAt = sentAt.AddDays(-1) } : null
        };
        Initialize(context, provider);

        // act
        await context.Endpoint.ExecuteAsync(context);
        using var item = await ReadQueueItemAsync(context);

        // assert
        Assert.Equal(sentAt, context.SentAt);
        Assert.Equal(sentAt, context.Envelope!.SentAt);
        Assert.Equal(sentAt, item.Envelope.SentAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_Should_PreserveOrDefaultSentAt_When_EnvelopeIsPrebuilt(bool hasSentAt)
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var envelope = new MessageEnvelope
        {
            SentAt = hasSentAt ? timeProvider.GetUtcNow().AddDays(-1) : null
        };
        var sentAt = envelope.SentAt ?? timeProvider.GetUtcNow();
        await using var provider = CreateProvider(timeProvider, null);
        var context = new DispatchContext { Envelope = envelope };
        Initialize(context, provider);

        // act
        await context.Endpoint.ExecuteAsync(context);
        using var item = await ReadQueueItemAsync(context);

        // assert
        Assert.Same(envelope, context.Envelope);
        Assert.Equal(sentAt, context.SentAt);
        Assert.Equal(sentAt, envelope.SentAt);
        Assert.Equal(sentAt, item.Envelope.SentAt);
    }

    [Fact]
    public async Task ExecuteAsync_Should_LeaveSentAtUnset_When_MiddlewareShortCircuits()
    {
        // arrange
        var middleware = new DispatchMiddlewareConfiguration(
            static (_, _) => static _ => ValueTask.CompletedTask,
            "CaptureMessage");
        await using var provider = CreateProvider(new FakeTimeProvider(), middleware);
        var context = new DispatchContext();
        Initialize(context, provider);

        // act
        await context.Endpoint.ExecuteAsync(context);

        // assert
        Assert.Null(context.SentAt);
        Assert.Null(context.Envelope);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseNewDispatchTime_When_ContextIsReused()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        await using var provider = CreateProvider(timeProvider, null);
        var pool = new DispatchContextPool();
        var firstContext = pool.Get();
        Initialize(firstContext, provider);
        await firstContext.Endpoint.ExecuteAsync(firstContext);
        using var firstItem = await ReadQueueItemAsync(firstContext);
        pool.Return(firstContext);
        timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var context = pool.Get();
        var sentAtAfterReset = context.SentAt;
        Initialize(context, provider);
        await context.Endpoint.ExecuteAsync(context);
        using var item = await ReadQueueItemAsync(context);

        // assert
        Assert.Same(firstContext, context);
        Assert.Null(sentAtAfterReset);
        Assert.Equal(timeProvider.GetUtcNow(), context.SentAt);
        Assert.Equal(timeProvider.GetUtcNow(), item.Envelope.SentAt);
        pool.Return(context);
    }

    [Fact]
    public async Task Initialize_Should_LeaveSentAtUnset_When_NoTimestampIsSupplied()
    {
        // arrange
        await using var provider = CreateProvider(new FakeTimeProvider(), null);
        var context = new DispatchContext();
        var initialSentAt = context.SentAt;

        // act
        Initialize(context, provider);

        // assert
        Assert.Null(initialSentAt);
        Assert.Null(context.SentAt);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseSystemTime_When_TimeProviderIsNotRegistered()
    {
        // arrange
        await using var provider = CreateProvider(null, null);
        var context = new DispatchContext();
        Initialize(context, provider);
        var beforeDispatch = DateTimeOffset.UtcNow;

        // act
        await context.Endpoint.ExecuteAsync(context);
        using var item = await ReadQueueItemAsync(context);

        // assert
        Assert.InRange(item.Envelope.SentAt!.Value, beforeDispatch, DateTimeOffset.UtcNow);
        Assert.Equal(context.SentAt, item.Envelope.SentAt);
    }

    private static ServiceProvider CreateProvider(
        TimeProvider? timeProvider,
        DispatchMiddlewareConfiguration? middleware)
    {
        var services = new ServiceCollection();
        if (timeProvider is not null)
        {
            services.AddSingleton(timeProvider);
        }

        var builder = services.AddMessageBus();
        builder.AddInMemory(t =>
        {
            t.DeclareQueue("sent-at");
            t.DispatchEndpoint("sent-at").ToQueue("sent-at").Publish<SentAtMessage>();
        });
        if (middleware is not null)
        {
            builder.ConfigureMessageBus(b => b.UseDispatch(middleware, after: "Serialization"));
        }

        return services.BuildServiceProvider();
    }

    private static void Initialize(DispatchContext context, IServiceProvider services)
    {
        var runtime = services.GetRequiredService<IMessagingRuntime>();
        var messageType = runtime.GetMessageType(typeof(SentAtMessage));
        var endpoint = runtime.GetPublishEndpoint(messageType);
        context.Message = new SentAtMessage();
        context.Initialize(services, endpoint, runtime, messageType, TestContext.Current.CancellationToken);
    }

    private static async Task<InMemoryQueueItem> ReadQueueItemAsync(DispatchContext context)
    {
        var endpoint = (InMemoryDispatchEndpoint)context.Endpoint;
        var queue = endpoint.Queue!;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        await using var messages = queue.ConsumeAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await messages.MoveNextAsync());
        return messages.Current;
    }

    public sealed record SentAtMessage;
}
