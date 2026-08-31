using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.Tests.Scheduling;

public class ScheduledMessageStoreResolverTests
{
    [Fact]
    public void TryResolve_Should_UseTransportSpecificStoreBeforeFallback()
    {
        var specific = new SpecificStore();
        var fallback = new FallbackStore();
        using var provider = BuildProvider(
            specific,
            fallback,
            new ScheduledMessageStoreRegistration(typeof(TransportA), "specific:", typeof(SpecificStore)),
            new ScheduledMessageStoreRegistration(null, "fallback:", typeof(FallbackStore), isFallback: true));
        var resolver = provider.GetRequiredService<ScheduledMessageStoreResolver>();
        var context = new DispatchContext { Transport = new TransportA() };

        var found = resolver.TryResolve(context, out var store);

        Assert.True(found);
        Assert.Same(specific, store);
    }

    [Fact]
    public void TryResolve_Should_UseMostSpecificTransportRegistration()
    {
        var specific = new SpecificStore();
        var fallback = new FallbackStore();
        using var provider = BuildProvider(
            specific,
            fallback,
            new ScheduledMessageStoreRegistration(typeof(StubTransport), "base:", typeof(FallbackStore)),
            new ScheduledMessageStoreRegistration(typeof(TransportA), "specific:", typeof(SpecificStore)));
        var resolver = provider.GetRequiredService<ScheduledMessageStoreResolver>();
        var context = new DispatchContext { Transport = new TransportA() };

        var found = resolver.TryResolve(context, out var store);

        Assert.True(found);
        Assert.Same(specific, store);
    }

    [Fact]
    public void TryResolve_Should_UseExactTransportInstanceBeforeTransportType()
    {
        var transport = new TransportA();
        var specific = new SpecificStore();
        var fallback = new FallbackStore();
        using var provider = BuildProvider(
            specific,
            fallback,
            new ScheduledMessageStoreRegistration(typeof(TransportA), "type:", typeof(FallbackStore)),
            new ScheduledMessageStoreRegistration(transport, "instance:", _ => specific));
        var resolver = provider.GetRequiredService<ScheduledMessageStoreResolver>();
        var context = new DispatchContext { Transport = transport };

        var found = resolver.TryResolve(context, out var store);

        Assert.True(found);
        Assert.Same(specific, store);
    }

    [Fact]
    public void TryResolve_Should_UseFallback_When_NoTransportSpecificStoreMatches()
    {
        var specific = new SpecificStore();
        var fallback = new FallbackStore();
        using var provider = BuildProvider(
            specific,
            fallback,
            new ScheduledMessageStoreRegistration(typeof(TransportA), "specific:", typeof(SpecificStore)),
            new ScheduledMessageStoreRegistration(null, "fallback:", typeof(FallbackStore), isFallback: true));
        var resolver = provider.GetRequiredService<ScheduledMessageStoreResolver>();
        var context = new DispatchContext { Transport = new TransportB() };

        var found = resolver.TryResolve(context, out var store);

        Assert.True(found);
        Assert.Same(fallback, store);
    }

    [Fact]
    public void TryResolve_Should_ReturnFalse_When_NoStoreMatches()
    {
        using var provider = BuildProvider(
            new SpecificStore(),
            new FallbackStore(),
            new ScheduledMessageStoreRegistration(typeof(TransportA), "specific:", typeof(SpecificStore)));
        var resolver = provider.GetRequiredService<ScheduledMessageStoreResolver>();
        var context = new DispatchContext { Transport = new TransportB() };

        var found = resolver.TryResolve(context, out _);

        Assert.False(found);
    }

    [Fact]
    public async Task CancelAsync_Should_DelegateToMatchingTokenPrefix()
    {
        var specific = new SpecificStore();
        var fallback = new FallbackStore();
        await using var provider = BuildProvider(
            specific,
            fallback,
            new ScheduledMessageStoreRegistration(typeof(TransportA), "specific:", typeof(SpecificStore)),
            new ScheduledMessageStoreRegistration(null, "fallback:", typeof(FallbackStore), isFallback: true));
        var resolver = provider.GetRequiredService<ScheduledMessageStoreResolver>();

        var cancelled = await resolver.CancelAsync("fallback:123", CancellationToken.None);

        Assert.True(cancelled);
        Assert.Null(specific.LastCancelledToken);
        Assert.Equal("fallback:123", fallback.LastCancelledToken);
    }

    [Fact]
    public async Task CancelAsync_Should_ReturnFalse_When_TokenPrefixIsUnknown()
    {
        await using var provider = BuildProvider(
            new SpecificStore(),
            new FallbackStore(),
            new ScheduledMessageStoreRegistration(typeof(TransportA), "specific:", typeof(SpecificStore)));
        var resolver = provider.GetRequiredService<ScheduledMessageStoreResolver>();

        var cancelled = await resolver.CancelAsync("unknown:123", CancellationToken.None);

        Assert.False(cancelled);
    }

    [Fact]
    public void Create_Should_RejectDuplicateTransportRegistrations()
    {
        using var provider = BuildProvider(
            new SpecificStore(),
            new FallbackStore(),
            new ScheduledMessageStoreRegistration(typeof(TransportA), "one:", typeof(SpecificStore)),
            new ScheduledMessageStoreRegistration(typeof(TransportA), "two:", typeof(SpecificStore)));

        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<ScheduledMessageStoreResolver>());
    }

    [Fact]
    public async Task CancelAsync_Should_TryAllStoresWithMatchingTokenPrefix()
    {
        var specific = new SpecificStore { CancelResult = false };
        var fallback = new FallbackStore { CancelResult = true };
        await using var provider = BuildProvider(
            specific,
            fallback,
            new ScheduledMessageStoreRegistration(new TransportA(), "same:", _ => specific),
            new ScheduledMessageStoreRegistration(new TransportA(), "same:", _ => fallback));
        var resolver = provider.GetRequiredService<ScheduledMessageStoreResolver>();

        var cancelled = await resolver.CancelAsync("same:123", CancellationToken.None);

        Assert.True(cancelled);
        Assert.Equal("same:123", specific.LastCancelledToken);
        Assert.Equal("same:123", fallback.LastCancelledToken);
    }

    [Fact]
    public void Create_Should_RejectOverlappingTokenPrefixes()
    {
        using var provider = BuildProvider(
            new SpecificStore(),
            new FallbackStore(),
            new ScheduledMessageStoreRegistration(typeof(TransportA), "same:", typeof(SpecificStore)),
            new ScheduledMessageStoreRegistration(typeof(TransportB), "same:nested:", typeof(FallbackStore)));

        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<ScheduledMessageStoreResolver>());
    }

    [Fact]
    public void Create_Should_RejectMultipleFallbackStores()
    {
        using var provider = BuildProvider(
            new SpecificStore(),
            new FallbackStore(),
            new ScheduledMessageStoreRegistration(null, "one:", typeof(SpecificStore), isFallback: true),
            new ScheduledMessageStoreRegistration(null, "two:", typeof(FallbackStore), isFallback: true));

        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<ScheduledMessageStoreResolver>());
    }

    [Fact]
    public void Create_Should_RejectInvalidStoreTypes()
    {
        using var provider = BuildProvider(
            new SpecificStore(),
            new FallbackStore(),
            new ScheduledMessageStoreRegistration(typeof(TransportA), "invalid:", typeof(string)));

        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<ScheduledMessageStoreResolver>());
    }

    private static ServiceProvider BuildProvider(
        SpecificStore specific,
        FallbackStore fallback,
        params ScheduledMessageStoreRegistration[] registrations)
    {
        var services = new ServiceCollection();
        services.AddScoped<ScheduledMessageStoreResolver>(ScheduledMessageStoreResolver.Create);
        services.AddSingleton(specific);
        services.AddSingleton(fallback);
        foreach (var registration in registrations)
        {
            services.AddSingleton(registration);
        }

        return services.BuildServiceProvider();
    }

    private class TestStore : IScheduledMessageStore
    {
        public string? LastCancelledToken { get; private set; }

        public bool CancelResult { get; init; } = true;

        public ValueTask<string> PersistAsync(IDispatchContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult("test:1");

        public ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
        {
            LastCancelledToken = token;
            return ValueTask.FromResult(CancelResult);
        }
    }

    private sealed class SpecificStore : TestStore;

    private sealed class FallbackStore : TestStore;

    private abstract class StubTransport : MessagingTransport
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

    private sealed class TransportA : StubTransport;

    private sealed class TransportB : StubTransport;
}
