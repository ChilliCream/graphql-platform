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
    public void Create_Should_ReturnNext_When_TransportHasSchedulingTransportFeature()
    {
        // arrange
        var features = new FeatureCollection();
        features.Set(new SchedulingTransportFeature { SupportsSchedulingNatively = true });
        var transport = new StubTransport(features);

        var appProvider = BuildAppProvider(registerStore: true);
        var busProvider = BuildBusProvider(appProvider);

        DispatchDelegate next = _ => ValueTask.CompletedTask;
        var config = DispatchSchedulingMiddleware.Create();
        var factoryContext = new DispatchMiddlewareFactoryContext
        {
            Services = busProvider,
            Endpoint = null!,
            Transport = transport
        };

        // act
        var result = config.Middleware(factoryContext, next);

        // assert - native feature → skip middleware
        Assert.Same(next, result);
    }

    [Fact]
    public void Create_Should_ReturnNext_When_NoScheduledMessageStoreRegistered()
    {
        // arrange
        var transport = new StubTransport(new FeatureCollection());

        var appProvider = BuildAppProvider(registerStore: false);
        var busProvider = BuildBusProvider(appProvider);

        DispatchDelegate next = _ => ValueTask.CompletedTask;
        var config = DispatchSchedulingMiddleware.Create();
        var factoryContext = new DispatchMiddlewareFactoryContext
        {
            Services = busProvider,
            Endpoint = null!,
            Transport = transport
        };

        // act
        var result = config.Middleware(factoryContext, next);

        // assert - no store registered → skip middleware
        Assert.Same(next, result);
    }

    [Fact]
    public void Create_Should_ReturnMiddleware_When_StoreRegisteredAndNoNativeFeature()
    {
        // arrange
        var transport = new StubTransport(new FeatureCollection());

        var appProvider = BuildAppProvider(registerStore: true);
        var busProvider = BuildBusProvider(appProvider);

        DispatchDelegate next = _ => ValueTask.CompletedTask;
        var config = DispatchSchedulingMiddleware.Create();
        var factoryContext = new DispatchMiddlewareFactoryContext
        {
            Services = busProvider,
            Endpoint = null!,
            Transport = transport
        };

        // act
        var result = config.Middleware(factoryContext, next);

        // assert - store registered, no native feature → install middleware
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
        var services = new ServiceCollection();
        services.AddScoped<IScheduledMessageStore>(_ => store);
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = new DispatchContext
        {
            ScheduledTime = timeProvider.GetUtcNow().AddMinutes(10),
            Services = scope.ServiceProvider,
            Envelope = new MessageEnvelope { MessageId = "msg-skip" }
        };

        // Set SkipScheduler flag
        context.Features.GetOrSet<SchedulingMiddlewareFeature>().SkipScheduler = true;

        var nextCalled = false;
        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert - next should be called, store should be empty
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
        var services = new ServiceCollection();
        services.AddScoped<IScheduledMessageStore>(_ => store);
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);
        var envelope = new MessageEnvelope { MessageId = "msg-persist" };
        var context = new DispatchContext
        {
            ScheduledTime = scheduledTime,
            Services = scope.ServiceProvider,
            Envelope = envelope
        };

        var nextCalled = false;
        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert - store should have the entry, next should NOT be called
        Assert.False(nextCalled);
        var entry = Assert.Single(store.Entries);
        Assert.Same(envelope, entry.Envelope);
        Assert.Equal(scheduledTime, entry.ScheduledTime);
    }

    [Fact]
    public async Task InvokeAsync_Should_ForwardToNext_When_EnvelopeIsNull()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var middleware = new DispatchSchedulingMiddleware();
        var store = new InMemoryScheduledMessageStore();
        var services = new ServiceCollection();
        services.AddScoped<IScheduledMessageStore>(_ => store);
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = new DispatchContext
        {
            ScheduledTime = timeProvider.GetUtcNow().AddMinutes(10),
            Services = scope.ServiceProvider,
            Envelope = null
        };

        var nextCalled = false;
        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert - store should be empty, next should be called as fallback
        Assert.True(nextCalled);
        Assert.Empty(store.Entries);
    }

    private static ServiceProvider BuildAppProvider(bool registerStore)
    {
        var services = new ServiceCollection();
        if (registerStore)
        {
            services.AddScoped<IScheduledMessageStore>(_ => new InMemoryScheduledMessageStore());
        }

        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildBusProvider(ServiceProvider appProvider)
    {
        var busServices = new ServiceCollection();
        busServices.AddSingleton<IRootServiceProviderAccessor>(
            new RootServiceProviderAccessor(appProvider));
        return busServices.BuildServiceProvider();
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

        public ValueTask PersistAsync(
            MessageEnvelope envelope,
            DateTimeOffset scheduledTime,
            CancellationToken cancellationToken)
        {
            Entries.Add((envelope, scheduledTime));
            return ValueTask.CompletedTask;
        }
    }
}
