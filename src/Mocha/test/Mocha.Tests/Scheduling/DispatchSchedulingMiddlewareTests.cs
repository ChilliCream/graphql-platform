using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.Tests.Scheduling;

public class DispatchSchedulingMiddlewareTests
{
    [Fact]
    public void Create_Should_AlwaysReturnMiddleware()
    {
        var transport = new StubTransport(new FeatureCollection());

        DispatchDelegate next = _ => ValueTask.CompletedTask;
        var config = DispatchSchedulingMiddleware.Create();
        var factoryContext = new DispatchMiddlewareFactoryContext
        {
            Services = null!,
            Endpoint = null!,
            Transport = transport
        };

        var result = config.Middleware(factoryContext, next);

        Assert.NotSame(next, result);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_ScheduledTimeIsNull()
    {
        var middleware = new DispatchSchedulingMiddleware();
        var context = new DispatchContext { ScheduledTime = null };
        var nextCalled = false;

        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        await middleware.InvokeAsync(context, next);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_SkipSchedulerIsTrue()
    {
        var timeProvider = new FakeTimeProvider();
        var middleware = new DispatchSchedulingMiddleware();
        var store = new InMemoryScheduledMessageStore();
        var transport = new StubTransport(new FeatureCollection());

        var services = BuildServices(transport, store);
        using var scope = services.CreateScope();

        var context = new DispatchContext
        {
            ScheduledTime = timeProvider.GetUtcNow().AddMinutes(10),
            Services = scope.ServiceProvider,
            Transport = transport,
            Envelope = new MessageEnvelope { MessageId = "msg-skip" }
        };

        context.Features.Configure<SchedulingMiddlewareFeature>(f => f.SkipScheduler = true);

        var nextCalled = false;
        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        await middleware.InvokeAsync(context, next);

        Assert.True(nextCalled);
        Assert.Empty(store.Entries);
    }

    [Fact]
    public async Task InvokeAsync_Should_PersistToStore_When_ScheduledTimeSetAndNotSkipped()
    {
        var timeProvider = new FakeTimeProvider();
        var middleware = new DispatchSchedulingMiddleware();
        var store = new InMemoryScheduledMessageStore();
        var transport = new StubTransport(new FeatureCollection());

        var services = BuildServices(transport, store);
        using var scope = services.CreateScope();

        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);
        var envelope = new MessageEnvelope { MessageId = "msg-persist", ScheduledTime = scheduledTime };
        var context = new DispatchContext
        {
            ScheduledTime = scheduledTime,
            Services = scope.ServiceProvider,
            Transport = transport,
            Envelope = envelope
        };

        var nextCalled = false;
        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        await middleware.InvokeAsync(context, next);

        Assert.False(nextCalled);
        var entry = Assert.Single(store.Entries);
        Assert.Same(envelope, entry.Envelope);
        Assert.Equal(scheduledTime, entry.ScheduledTime);
    }

    [Fact]
    public async Task InvokeAsync_Should_ForwardToNext_When_EnvelopeIsNull()
    {
        var timeProvider = new FakeTimeProvider();
        var middleware = new DispatchSchedulingMiddleware();
        var store = new InMemoryScheduledMessageStore();
        var transport = new StubTransport(new FeatureCollection());
        var services = BuildServices(transport, store);
        using var scope = services.CreateScope();

        var context = new DispatchContext
        {
            ScheduledTime = timeProvider.GetUtcNow().AddMinutes(10),
            Services = scope.ServiceProvider,
            Transport = transport,
            Envelope = null
        };

        var nextCalled = false;
        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        await middleware.InvokeAsync(context, next);

        Assert.True(nextCalled);
        Assert.Empty(store.Entries);
    }

    [Fact]
    public async Task InvokeAsync_Should_Throw_When_NoStoreRegistered()
    {
        var timeProvider = new FakeTimeProvider();
        var middleware = new DispatchSchedulingMiddleware();
        var transport = new StubTransport(new FeatureCollection());

        var services = new ServiceCollection();
        services.AddScoped<IScheduledMessageStoreResolver, ScheduledMessageStoreResolver>();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);
        var context = new DispatchContext
        {
            ScheduledTime = scheduledTime,
            Services = scope.ServiceProvider,
            Transport = transport,
            Envelope = new MessageEnvelope { MessageId = "msg", ScheduledTime = scheduledTime }
        };

        DispatchDelegate next = _ => ValueTask.CompletedTask;

        await Assert.ThrowsAsync<NotSupportedException>(() => middleware.InvokeAsync(context, next).AsTask());
    }

    private static ServiceProvider BuildServices(StubTransport transport, InMemoryScheduledMessageStore store)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        services.AddSingleton(
            new ScheduledMessageStoreRegistration(
                TransportType: typeof(StubTransport),
                TokenPrefix: "in-memory:",
                StoreType: typeof(InMemoryScheduledMessageStore)));
        services.AddScoped<IScheduledMessageStoreResolver, ScheduledMessageStoreResolver>();
        return services.BuildServiceProvider();
    }

    private sealed class StubTransport : MessagingTransport
    {
        private static readonly FieldInfo s_featuresField =
            typeof(MessagingTransport).GetField("_features", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public StubTransport(IFeatureCollection features)
        {
            s_featuresField.SetValue(this, features);
        }

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
        public ConcurrentBag<(MessageEnvelope Envelope, DateTimeOffset ScheduledTime)> Entries { get; } = [];

        public ValueTask<string> PersistAsync(IDispatchContext context, CancellationToken cancellationToken)
        {
            var envelope = context.Envelope!;
            Entries.Add((envelope, envelope.ScheduledTime!.Value));
            var token = $"in-memory:{Guid.NewGuid()}";
            return ValueTask.FromResult(token);
        }

        public ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(false);
        }
    }
}
