using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.Tests.Scheduling;

public class ScheduledMessageStoreResolverTests
{
    [Fact]
    public void TryGetForDispatch_Should_PreferTransportSpecificStore_When_BothRegistered()
    {
        var transportStore = new StubStore("transport:");
        var fallbackStore = new FallbackStore();

        var services = new ServiceCollection();
        services.AddSingleton(transportStore);
        services.AddSingleton(fallbackStore);
        services.AddSingleton(
            new ScheduledMessageStoreRegistration(typeof(StubTransport), "transport:", typeof(StubStore)));
        services.AddSingleton(
            new ScheduledMessageStoreRegistration(null, "fallback:", typeof(FallbackStore), IsFallback: true));

        var provider = services.BuildServiceProvider();
        var resolver = new ScheduledMessageStoreResolver(
            provider,
            provider.GetServices<ScheduledMessageStoreRegistration>());

        var ctx = new DispatchContext { Transport = new StubTransport() };

        Assert.True(resolver.TryGetForDispatch(ctx, out var store));
        Assert.Same(transportStore, store);
    }

    [Fact]
    public void TryGetForDispatch_Should_FallBack_When_NoTransportSpecificMatch()
    {
        var fallbackStore = new FallbackStore();

        var services = new ServiceCollection();
        services.AddSingleton(fallbackStore);
        services.AddSingleton(
            new ScheduledMessageStoreRegistration(null, "fallback:", typeof(FallbackStore), IsFallback: true));

        var provider = services.BuildServiceProvider();
        var resolver = new ScheduledMessageStoreResolver(
            provider,
            provider.GetServices<ScheduledMessageStoreRegistration>());

        var ctx = new DispatchContext { Transport = new StubTransport() };

        Assert.True(resolver.TryGetForDispatch(ctx, out var store));
        Assert.Same(fallbackStore, store);
    }

    [Fact]
    public void TryGetForDispatch_Should_ReturnFalse_When_NoMatchAndNoFallback()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var resolver = new ScheduledMessageStoreResolver(
            provider,
            provider.GetServices<ScheduledMessageStoreRegistration>());

        var ctx = new DispatchContext { Transport = new StubTransport() };

        Assert.False(resolver.TryGetForDispatch(ctx, out _));
    }

    [Fact]
    public void TryGetForCancellation_Should_RouteByTokenPrefix()
    {
        var transportStore = new StubStore("transport:");
        var fallbackStore = new FallbackStore();

        var services = new ServiceCollection();
        services.AddSingleton(transportStore);
        services.AddSingleton(fallbackStore);
        services.AddSingleton(
            new ScheduledMessageStoreRegistration(typeof(StubTransport), "transport:", typeof(StubStore)));
        services.AddSingleton(
            new ScheduledMessageStoreRegistration(null, "fallback:", typeof(FallbackStore), IsFallback: true));

        var provider = services.BuildServiceProvider();
        var resolver = new ScheduledMessageStoreResolver(
            provider,
            provider.GetServices<ScheduledMessageStoreRegistration>());

        Assert.True(resolver.TryGetForCancellation("transport:abc", out var s1));
        Assert.Same(transportStore, s1);

        Assert.True(resolver.TryGetForCancellation("fallback:abc", out var s2));
        Assert.Same(fallbackStore, s2);
    }

    [Fact]
    public void TryGetForCancellation_Should_ReturnFalse_When_PrefixUnknown()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new StubStore("transport:"));
        services.AddSingleton(
            new ScheduledMessageStoreRegistration(typeof(StubTransport), "transport:", typeof(StubStore)));

        var provider = services.BuildServiceProvider();
        var resolver = new ScheduledMessageStoreResolver(
            provider,
            provider.GetServices<ScheduledMessageStoreRegistration>());

        Assert.False(resolver.TryGetForCancellation("unknown:abc", out _));
    }

    [Fact]
    public void Constructor_Should_Throw_When_DuplicateTransportRegistrations()
    {
        var registrations = new[]
        {
            new ScheduledMessageStoreRegistration(typeof(StubTransport), "a:", typeof(StubStore)),
            new ScheduledMessageStoreRegistration(typeof(StubTransport), "b:", typeof(StubStore))
        };

        var services = new ServiceCollection().BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(
            () => new ScheduledMessageStoreResolver(services, registrations));
    }

    [Fact]
    public void Constructor_Should_Throw_When_DuplicateTokenPrefix()
    {
        var registrations = new[]
        {
            new ScheduledMessageStoreRegistration(typeof(StubTransport), "same:", typeof(StubStore)),
            new ScheduledMessageStoreRegistration(typeof(StubTransport2), "same:", typeof(StubStore))
        };

        var services = new ServiceCollection().BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(
            () => new ScheduledMessageStoreResolver(services, registrations));
    }

    [Fact]
    public void Constructor_Should_Throw_When_MultipleFallbackRegistrations()
    {
        var registrations = new[]
        {
            new ScheduledMessageStoreRegistration(null, "a:", typeof(StubStore), IsFallback: true),
            new ScheduledMessageStoreRegistration(null, "b:", typeof(FallbackStore), IsFallback: true)
        };

        var services = new ServiceCollection().BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(
            () => new ScheduledMessageStoreResolver(services, registrations));
    }

    [Fact]
    public void Constructor_Should_Throw_When_StoreTypeDoesNotImplementInterface()
    {
        var registrations = new[]
        {
            new ScheduledMessageStoreRegistration(typeof(StubTransport), "a:", typeof(string))
        };

        var services = new ServiceCollection().BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(
            () => new ScheduledMessageStoreResolver(services, registrations));
    }

    private sealed class StubStore : IScheduledMessageStore
    {
        public StubStore(string prefix) { _ = prefix; }

        public ValueTask<string> PersistAsync(IDispatchContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(string.Empty);

        public ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
            => ValueTask.FromResult(true);
    }

    private sealed class FallbackStore : IScheduledMessageStore
    {
        public ValueTask<string> PersistAsync(IDispatchContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(string.Empty);

        public ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
            => ValueTask.FromResult(true);
    }

    private sealed class StubTransport : MessagingTransport
    {
        private static readonly FieldInfo s_featuresField =
            typeof(MessagingTransport).GetField("_features", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public StubTransport()
        {
            s_featuresField.SetValue(this, new FeatureCollection());
        }

        public override MessagingTopology Topology => null!;

        public override bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
        {
            endpoint = null;
            return false;
        }

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context, OutboundRoute route) => null;

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context, Uri address) => null;

        public override ReceiveEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context, InboundRoute route) => null;

        protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context) => null!;

        protected override ReceiveEndpoint CreateReceiveEndpoint() => null!;

        protected override DispatchEndpoint CreateDispatchEndpoint() => null!;
    }

    private sealed class StubTransport2 : MessagingTransport
    {
        private static readonly FieldInfo s_featuresField =
            typeof(MessagingTransport).GetField("_features", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public StubTransport2()
        {
            s_featuresField.SetValue(this, new FeatureCollection());
        }

        public override MessagingTopology Topology => null!;

        public override bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
        {
            endpoint = null;
            return false;
        }

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context, OutboundRoute route) => null;

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context, Uri address) => null;

        public override ReceiveEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context, InboundRoute route) => null;

        protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context) => null!;

        protected override ReceiveEndpoint CreateReceiveEndpoint() => null!;

        protected override DispatchEndpoint CreateDispatchEndpoint() => null!;
    }
}
