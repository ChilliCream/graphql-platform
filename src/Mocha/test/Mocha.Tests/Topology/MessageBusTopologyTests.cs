using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Topology;

public sealed class MessageBusTopologyTests
{
    [Fact]
    public async Task Description_Should_ReturnRegisteredTopology_When_MessageBusConfigured()
    {
        // arrange
        await using var provider = CreateProvider(b => b.AddEventHandler<TestEventHandler>());
        var topology = provider.GetRequiredService<IMessageBusTopology>();

        // act
        var description = topology.Description;

        // assert
        var transport = Assert.Single(description.Transports);
        Assert.Contains(description.MessageTypes, mt => mt.RuntimeType == nameof(TestEvent));
        Assert.Contains(description.Consumers, c => c.Name == nameof(TestEventHandler));
        Assert.NotEmpty(transport.Topology!.Entities);
    }

    [Fact]
    public async Task Description_Should_IncludeDispatchEndpoint_When_OutboundRouteUsesConfiguredDestination()
    {
        // arrange
        await using var provider = CreateProvider(
            b => b.AddMessage<TestCommand>(m => m.Send(r => r.ToQueue("configured-command"))));
        var topology = provider.GetRequiredService<IMessageBusTopology>();

        // act
        var description = topology.Description;

        // assert
        var transport = Assert.Single(description.Transports);
        Assert.Contains(
            transport.DispatchEndpoints,
            e => e is { Name: "q/configured-command", Kind: DispatchEndpointKind.Default }
                && e.DestinationAddress?.EndsWith("/q/configured-command", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task GetChangeToken_Should_Fire_When_LazyDispatchEndpointCreated()
    {
        // arrange
        await using var provider = CreateProvider(b => b.AddEventHandler<TestEventHandler>());
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var token = runtime.Endpoints.GetChangeToken();
        var fired = false;
        token.RegisterChangeCallback(_ => fired = true, null);

        // act
        runtime.GetDispatchEndpoint(new Uri("queue:lazy-dispatch-endpoint"));

        // assert
        Assert.True(fired);
    }

    [Fact]
    public async Task GetChangeToken_Should_ExposeNewToken_When_EndpointRouterCallbackRuns()
    {
        // arrange
        await using var provider = CreateProvider(b => b.AddEventHandler<TestEventHandler>());
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var token = runtime.Endpoints.GetChangeToken();
        IChangeToken? nextToken = null;
        token.RegisterChangeCallback(_ => nextToken = runtime.Endpoints.GetChangeToken(), null);

        // act
        runtime.GetDispatchEndpoint(new Uri("queue:swap-dispatch-endpoint"));

        // assert
        Assert.NotNull(nextToken);
        Assert.NotSame(token, nextToken);
        Assert.False(nextToken!.HasChanged);
    }

    [Fact]
    public async Task Description_Should_InvalidateCache_When_LazyDispatchEndpointCreated()
    {
        // arrange
        await using var provider = CreateProvider(b => b.AddEventHandler<TestEventHandler>());
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var topology = provider.GetRequiredService<IMessageBusTopology>();
        var before = topology.Description;
        var beforeCount = before.Transports.Sum(t => t.DispatchEndpoints.Count);
        var token = topology.GetChangeToken();
        var fired = false;
        token.RegisterChangeCallback(_ => fired = true, null);

        // act
        runtime.GetDispatchEndpoint(new Uri("queue:topology-lazy-dispatch-endpoint"));
        var after = topology.Description;

        // assert
        Assert.True(fired);
        Assert.NotSame(token, topology.GetChangeToken());
        Assert.NotSame(before, after);
        Assert.True(after.Transports.Sum(t => t.DispatchEndpoints.Count) > beforeCount);
    }

    [Fact]
    public void GetChangeToken_Should_Fire_When_TransportTokenFires()
    {
        // arrange
        var transport = new MutableTokenTransport();
        var runtime = new FakeRuntime(new EndpointRouter(), [transport]);
        using var topology = new MessageBusTopology(runtime);
        var token = topology.GetChangeToken();
        IChangeToken? nextToken = null;
        token.RegisterChangeCallback(_ => nextToken = topology.GetChangeToken(), null);

        // act
        transport.SignalChange();

        // assert
        Assert.NotNull(nextToken);
        Assert.NotSame(token, nextToken);
        Assert.False(nextToken!.HasChanged);
    }

    private static ServiceProvider CreateProvider(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();
        return services.BuildServiceProvider();
    }

    private sealed class TestEvent;

    private sealed class TestCommand;

    private sealed class TestEventHandler : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken ct) => default;
    }

    private sealed class MutableTokenTransport : MessagingTransport
    {
        private readonly object _lock = new();
        private CancellationTokenSource _changeTokenSource = new();

        public override MessagingTopology Topology => throw new NotSupportedException();

        public override IChangeToken GetChangeToken()
        {
            lock (_lock)
            {
                return new CancellationChangeToken(_changeTokenSource.Token);
            }
        }

        public void SignalChange()
        {
            CancellationTokenSource changeTokenSource;

            lock (_lock)
            {
                changeTokenSource = _changeTokenSource;
                _changeTokenSource = new CancellationTokenSource();
            }

            changeTokenSource.Cancel();
            changeTokenSource.Dispose();
        }

        internal override TransportDescription Describe()
            => new("test:/", "test", "test", nameof(MutableTokenTransport), [], [], null);

        public override bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
        {
            endpoint = null;
            return false;
        }

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            OutboundRoute route)
            => throw new NotSupportedException();

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            Uri address)
            => throw new NotSupportedException();

        public override ReceiveEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            InboundRoute route)
            => throw new NotSupportedException();

        protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
            => throw new NotSupportedException();

        protected override ReceiveEndpoint CreateReceiveEndpoint()
            => throw new NotSupportedException();

        protected override DispatchEndpoint CreateDispatchEndpoint()
            => throw new NotSupportedException();
    }

    private sealed class FakeRuntime(IEndpointRouter endpoints, ImmutableArray<MessagingTransport> transports)
        : IMessagingRuntime
    {
        public IServiceProvider Services => throw new NotSupportedException();

        public IBusNamingConventions Naming => throw new NotSupportedException();

        public IMessageTypeRegistry Messages => throw new NotSupportedException();

        public IMessageRouter Router => throw new NotSupportedException();

        public IEndpointRouter Endpoints => endpoints;

        public IHostInfo Host => throw new NotSupportedException();

        public IConventionRegistry Conventions => throw new NotSupportedException();

        public ImmutableHashSet<Consumer> Consumers => [];

        public ImmutableArray<MessagingTransport> Transports => transports;

        public IFeatureCollection Features => throw new NotSupportedException();

        public IReadOnlyMessagingOptions Options => throw new NotSupportedException();

        public DispatchEndpoint GetSendEndpoint(MessageType messageType) => throw new NotSupportedException();

        public DispatchEndpoint GetPublishEndpoint(MessageType messageType) => throw new NotSupportedException();

        public DispatchEndpoint GetDispatchEndpoint(Uri address) => throw new NotSupportedException();

        public MessageType GetMessageType(Type type) => throw new NotSupportedException();

        public MessageType? GetMessageType(string? identity) => throw new NotSupportedException();

        public MessagingTransport? GetTransport(Uri address) => throw new NotSupportedException();
    }
}
