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
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var topology = runtime.Topology;

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
        await using var provider = CreateProvider(b =>
            b.AddMessage<TestCommand>(m => m.Send(r => r.ToQueue("configured-command")))
        );
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var topology = runtime.Topology;

        // act
        var description = topology.Description;

        // assert
        var transport = Assert.Single(description.Transports);
        Assert.Contains(
            transport.DispatchEndpoints,
            e =>
                e is { Name: "q/configured-command", Kind: DispatchEndpointKind.Default }
                && e.DestinationAddress?.EndsWith("/q/configured-command", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task GetDispatchEndpoint_Should_ReturnExistingReplyEndpoint_When_ReplyAliasUsed()
    {
        // arrange
        await using var provider = CreateProvider(b => b.AddEventHandler<TestEventHandler>());
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var topology = runtime.Topology;
        var transport = runtime.Transports.Single();
        var beforeCount = transport.DispatchEndpoints.Count;
        var beforeReplyCount = topology
            .Description.Transports.Single()
            .DispatchEndpoints.Count(e => e.Kind == DispatchEndpointKind.Reply);

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("memory:replies"));

        // assert
        Assert.Same(transport.ReplyDispatchEndpoint, endpoint);
        Assert.Equal(beforeCount, transport.DispatchEndpoints.Count);
        Assert.Equal(
            beforeReplyCount,
            topology
                .Description.Transports.Single()
                .DispatchEndpoints.Count(e => e.Kind == DispatchEndpointKind.Reply));
    }

    [Fact]
    public async Task GetEndpoint_Should_CompleteLazyOutboundRoute_When_RuntimeSendEndpointCreated()
    {
        // arrange
        await using var provider = CreateProvider(_ => { });
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var messageType = runtime.GetMessageType(typeof(LazyCommand));

        // act
        var endpoint = runtime.GetSendEndpoint(messageType);

        // assert
        var route = Assert.Single(
            runtime.Router.GetOutboundByMessageType(messageType),
            r => r.Kind == OutboundRouteKind.Send);
        Assert.True(route.IsCompleted);
        Assert.Same(endpoint, route.Endpoint);
        Assert.Equal(endpoint.Address, route.Destination);
    }

    [Fact]
    public async Task GetEndpoint_Should_ReturnSameCompletedEndpoint_When_AddressLookupRacesWithRouteLookup()
    {
        // arrange
        await using var provider = CreateProvider(_ => { });
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var messageType = runtime.GetMessageType(typeof(LazyCommand));
        var address = new Uri($"queue:{runtime.Naming.GetSendEndpointName(typeof(LazyCommand))}");

        // act
        var endpoints = await Task.WhenAll(
            Enumerable
                .Range(0, 20)
                .Select(i =>
                    Task.Run(() =>
                        i % 2 == 0 ? runtime.GetSendEndpoint(messageType) : runtime.GetDispatchEndpoint(address)
                    )
                ));

        // assert
        var endpoint = Assert.Single(endpoints.Distinct());
        Assert.True(endpoint.IsCompleted);
        Assert.Equal(1, runtime.Transports.Single().DispatchEndpoints.Count(e => e.Name == endpoint.Name));
    }

    [Fact]
    public async Task GetEndpoint_Should_ThrowNoTransportForMessageType_When_NoTransportExists()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus();
        await using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var messageType = runtime.GetMessageType(typeof(NoTransportCommand));

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => runtime.GetSendEndpoint(messageType));

        // assert
        Assert.Contains("No transport can handle message type", exception.Message);
    }

    [Fact]
    public async Task GetEndpoint_Should_PreserveExplicitDestination_When_ConfiguredEndpointMatchesMessage()
    {
        // arrange
        await using var provider = CreateProvider(
            b => b.AddMessage<ExplicitDestinationCommand>(m => m.Send(r => r.ToQueue("configured-command"))),
            d => d.DispatchEndpoint("configured-endpoint").ToQueue("other-command").Send<ExplicitDestinationCommand>());
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var messageType = runtime.GetMessageType(typeof(ExplicitDestinationCommand));

        // act
        var route = Assert.Single(
            runtime.Router.GetOutboundByMessageType(messageType),
            r => r.Kind == OutboundRouteKind.Send);

        // assert
        Assert.Contains("configured-command", route.Destination!.ToString());
        Assert.DoesNotContain("other-command", route.Destination.ToString());
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
        var topology = runtime.Topology;
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

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_RuntimeIsNull()
    {
        // act
        var exception = Assert.Throws<ArgumentNullException>(() => new MessageBusTopology(null!));

        // assert
        Assert.Equal("runtime", exception.ParamName);
    }

    [Fact]
    public async Task Description_Should_ReturnSameInstance_When_CalledMultipleTimes()
    {
        // arrange
        await using var provider = CreateProvider(b => b.AddEventHandler<TestEventHandler>());
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var topology = runtime.Topology;

        // act
        var first = topology.Description;
        var second = topology.Description;

        // assert
        Assert.Same(first, second);
    }

    [Fact]
    public void Description_Should_ThrowObjectDisposedException_When_Disposed()
    {
        // arrange
        var transport = new MutableTokenTransport();
        var runtime = new FakeRuntime(new EndpointRouter(), [transport]);
        var topology = new MessageBusTopology(runtime);
        topology.Dispose();

        // act & assert
        Assert.Throws<ObjectDisposedException>(() => topology.Description);
    }

    [Fact]
    public void GetChangeToken_Should_ThrowObjectDisposedException_When_Disposed()
    {
        // arrange
        var transport = new MutableTokenTransport();
        var runtime = new FakeRuntime(new EndpointRouter(), [transport]);
        var topology = new MessageBusTopology(runtime);
        topology.Dispose();

        // act & assert
        Assert.Throws<ObjectDisposedException>(() => topology.GetChangeToken());
    }

    [Fact]
    public void GetChangeToken_Should_ReturnUnchangedToken_When_NothingHasChanged()
    {
        // arrange
        var transport = new MutableTokenTransport();
        var runtime = new FakeRuntime(new EndpointRouter(), [transport]);
        using var topology = new MessageBusTopology(runtime);

        // act
        var token = topology.GetChangeToken();

        // assert
        Assert.False(token.HasChanged);
        Assert.True(token.ActiveChangeCallbacks);
    }

    [Fact]
    public void Dispose_Should_NotThrow_When_CalledMultipleTimes()
    {
        // arrange
        var transport = new MutableTokenTransport();
        var runtime = new FakeRuntime(new EndpointRouter(), [transport]);
        var topology = new MessageBusTopology(runtime);

        // act
        topology.Dispose();
        var exception = Record.Exception(() => topology.Dispose());

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task Invalidate_Should_ClearCachedDescription_When_TransportTokenFires()
    {
        // arrange
        await using var provider = CreateProvider(b => b.AddEventHandler<TestEventHandler>());
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var topology = runtime.Topology;
        var first = topology.Description;

        // act
        // Signal change via the endpoint router (creating a lazy endpoint invalidates the topology)
        runtime.GetDispatchEndpoint(new Uri("queue:invalidate-test-endpoint"));
        var second = topology.Description;

        // assert
        Assert.NotSame(first, second);
    }

    private static ServiceProvider CreateProvider(
        Action<IMessageBusHostBuilder> configure,
        Action<IInMemoryMessagingTransportDescriptor>? configureInMemory = null)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        if (configureInMemory is null)
        {
            builder.AddInMemory();
        }
        else
        {
            builder.AddInMemory(configureInMemory);
        }

        return services.BuildServiceProvider();
    }

    private sealed class TestEvent;

    private sealed class TestCommand;

    private sealed class LazyCommand;

    private sealed class NoTransportCommand;

    private sealed class ExplicitDestinationCommand;

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

        public override TransportDescription Describe()
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

        protected override ReceiveEndpoint CreateReceiveEndpoint() => throw new NotSupportedException();

        protected override DispatchEndpoint CreateDispatchEndpoint() => throw new NotSupportedException();
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

        public IMessageBusTopology Topology => throw new NotSupportedException();

        public DispatchEndpoint GetSendEndpoint(MessageType messageType) => throw new NotSupportedException();

        public DispatchEndpoint GetPublishEndpoint(MessageType messageType) => throw new NotSupportedException();

        public DispatchEndpoint GetDispatchEndpoint(Uri address) => throw new NotSupportedException();

        public MessageType GetMessageType(Type type) => throw new NotSupportedException();

        public MessageType? GetMessageType(string? identity) => throw new NotSupportedException();

        public MessagingTransport? GetTransport(Uri address) => throw new NotSupportedException();
    }
}
