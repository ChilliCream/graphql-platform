using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.Tests.Scheduling;

public class DispatchSchedulingMiddlewareTests
{
    [Fact]
    public void Create_Should_ReturnMiddleware()
    {
        // arrange
        DispatchDelegate next = _ => ValueTask.CompletedTask;
        var config = DispatchSchedulingMiddleware.Create();
        var factoryContext = new DispatchMiddlewareFactoryContext
        {
            Services = new ServiceCollection().BuildServiceProvider(),
            Endpoint = null!,
            Transport = new StubTransport()
        };

        // act
        var result = config.Middleware(factoryContext, next);

        // assert
        Assert.NotSame(next, result);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_ScheduledTimeIsNull()
    {
        // arrange
        var middleware = new DispatchSchedulingMiddleware();
        var context = new DispatchContext { ScheduledTime = null };
        var nextCalled = false;

        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_SkipSchedulerIsTrue()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var middleware = new DispatchSchedulingMiddleware();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = BuildProvider(store);
        using var scope = provider.CreateScope();

        var context = new DispatchContext
        {
            ScheduledTime = timeProvider.GetUtcNow().AddMinutes(10),
            Services = scope.ServiceProvider,
            Envelope = new MessageEnvelope { MessageId = "msg-skip" },
            Transport = new StubTransport()
        };

        context.Features.Configure<SchedulingMiddlewareFeature>(f => f.SkipScheduler = true);

        var nextCalled = false;
        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(nextCalled);
        Assert.Empty(store.Entries);
    }

    [Fact]
    public async Task InvokeAsync_Should_PersistToStore_When_ScheduledTimeSetAndNotSkipped()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var middleware = new DispatchSchedulingMiddleware();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = BuildProvider(store);
        using var scope = provider.CreateScope();

        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);
        var envelope = new MessageEnvelope { MessageId = "msg-persist", ScheduledTime = scheduledTime };
        var context = new DispatchContext
        {
            ScheduledTime = scheduledTime,
            Services = scope.ServiceProvider,
            Envelope = envelope,
            Transport = new StubTransport()
        };

        var nextCalled = false;
        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.False(nextCalled);
        var entry = Assert.Single(store.Entries);
        Assert.Same(envelope, entry.Envelope);
        Assert.Equal(scheduledTime, entry.ScheduledTime);
    }

    [Fact]
    public async Task InvokeAsync_Should_CopyScheduledTimeToEnvelope_When_PreBuiltEnvelopeHasNoScheduledTime()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var middleware = new DispatchSchedulingMiddleware();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = BuildProvider(store);
        using var scope = provider.CreateScope();

        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);
        var envelope = new MessageEnvelope { MessageId = "msg-persist" };
        var context = new DispatchContext
        {
            ScheduledTime = scheduledTime,
            Services = scope.ServiceProvider,
            Envelope = envelope,
            Transport = new StubTransport()
        };

        // act
        await middleware.InvokeAsync(context, _ => ValueTask.CompletedTask);

        // assert
        var entry = Assert.Single(store.Entries);
        Assert.NotSame(envelope, entry.Envelope);
        Assert.Equal(scheduledTime, entry.Envelope.ScheduledTime);
    }

    [Fact]
    public async Task InvokeAsync_Should_OverwriteEnvelopeScheduledTime_When_ContextScheduledTimeDiffers()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var middleware = new DispatchSchedulingMiddleware();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = BuildProvider(store);
        using var scope = provider.CreateScope();

        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);
        var envelope = new MessageEnvelope
        {
            MessageId = "msg-persist",
            ScheduledTime = scheduledTime.AddMinutes(-5)
        };
        var context = new DispatchContext
        {
            ScheduledTime = scheduledTime,
            Services = scope.ServiceProvider,
            Envelope = envelope,
            Transport = new StubTransport()
        };

        // act
        await middleware.InvokeAsync(context, _ => ValueTask.CompletedTask);

        // assert
        var entry = Assert.Single(store.Entries);
        Assert.NotSame(envelope, entry.Envelope);
        Assert.Equal(scheduledTime, entry.Envelope.ScheduledTime);
    }

    [Fact]
    public async Task InvokeAsync_Should_ThrowNotSupported_When_NoStoreMatchesTransport()
    {
        // arrange
        var middleware = new DispatchSchedulingMiddleware();
        var services = new ServiceCollection();
        services.AddScoped<ScheduledMessageStoreResolver>(ScheduledMessageStoreResolver.Create);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var scheduledTime = DateTimeOffset.UtcNow.AddMinutes(10);
        var context = new DispatchContext
        {
            ScheduledTime = scheduledTime,
            Services = scope.ServiceProvider,
            Envelope = new MessageEnvelope { MessageId = "msg-unsupported", ScheduledTime = scheduledTime },
            Transport = new StubTransport()
        };

        // act + assert
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await middleware.InvokeAsync(context, _ => ValueTask.CompletedTask));
    }

    [Fact]
    public async Task InvokeAsync_Should_ForwardToNext_When_EnvelopeIsNull()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var middleware = new DispatchSchedulingMiddleware();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = BuildProvider(store);
        using var scope = provider.CreateScope();

        var context = new DispatchContext
        {
            ScheduledTime = timeProvider.GetUtcNow().AddMinutes(10),
            Services = scope.ServiceProvider,
            Envelope = null,
            Transport = new StubTransport()
        };

        var nextCalled = false;
        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(nextCalled);
        Assert.Empty(store.Entries);
    }

    private static ServiceProvider BuildProvider(InMemoryScheduledMessageStore store)
    {
        var services = new ServiceCollection();
        services.AddScoped<ScheduledMessageStoreResolver>(ScheduledMessageStoreResolver.Create);
        services.AddScoped(_ => store);
        services.AddSingleton(
            new ScheduledMessageStoreRegistration(
                typeof(StubTransport),
                InMemoryScheduledMessageStore.TokenPrefix,
                typeof(InMemoryScheduledMessageStore)));
        return services.BuildServiceProvider();
    }

    private sealed class StubTransport : MessagingTransport
    {
        public override MessagingTopology Topology => null!;

        public override bool TryGetDispatchEndpoint(
            Uri address,
            [NotNullWhen(true)] out DispatchEndpoint? endpoint)
        {
            endpoint = null;
            return false;
        }

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            OutboundRoute route) => null;

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            Uri address) => null;

        public override ReceiveEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            InboundRoute route) => null;

        protected override MessagingTransportConfiguration CreateConfiguration(
            IMessagingSetupContext context) => null!;

        protected override ReceiveEndpoint CreateReceiveEndpoint() => null!;

        protected override DispatchEndpoint CreateDispatchEndpoint() => null!;
    }

    private sealed class InMemoryScheduledMessageStore : IScheduledMessageStore
    {
        public const string TokenPrefix = "in-memory:";

        public ConcurrentBag<(MessageEnvelope Envelope, DateTimeOffset ScheduledTime)> Entries { get; } = [];

        public ValueTask<string> PersistAsync(
            IDispatchContext context,
            CancellationToken cancellationToken)
        {
            var envelope = context.Envelope ?? throw new InvalidOperationException("Envelope is not set");
            var scheduledTime = envelope.ScheduledTime
                ?? throw new InvalidOperationException("Scheduled time is not set");
            Entries.Add((envelope, scheduledTime));
            var token = $"{TokenPrefix}{Guid.NewGuid()}";
            return ValueTask.FromResult(token);
        }

        public ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(false);
        }
    }
}
