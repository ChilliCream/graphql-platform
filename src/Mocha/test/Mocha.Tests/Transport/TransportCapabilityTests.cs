using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Transport;

public class TransportCapabilityTests
{
    [Fact]
    public void Capabilities_Should_BeAll_When_DefaultTransport()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<TestEventHandler>());
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act
        var capabilities = transport.Capabilities;

        // assert
        Assert.Equal(MessagingTransportCapabilities.All, capabilities);
    }

    [Fact]
    public void TransportDescription_Should_IncludeCapabilities_When_DefaultTransport()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<TestEventHandler>());
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        Assert.Equal(MessagingTransportCapabilities.All, description.Capabilities);
    }

    [Fact]
    public void BuildRuntime_Should_Succeed_When_RequestHandlerRegisteredWithInMemory()
    {
        // arrange
        var runtime = CreateRuntime(b =>
        {
            b.Services.AddSingleton(new MessageRecorder());
            b.AddRequestHandler<TestRequestHandler>();
        });

        // act
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        Assert.True(transport.HasCapability(MessagingTransportCapabilities.RequestReply));
    }

    [Fact]
    public void DiscoverEndpoints_Should_CreateReplyReceiveEndpoint_When_RequestHandlerRegisteredWithInMemory()
    {
        // arrange
        var runtime = CreateRuntime(b =>
        {
            b.Services.AddSingleton(new MessageRecorder());
            b.AddRequestHandler<TestRequestHandler>();
        });

        // act
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        Assert.Contains(transport.ReceiveEndpoints, e => e.Kind == ReceiveEndpointKind.Reply);
    }

    [Fact]
    public void DiscoverEndpoints_Should_CreateMatchingSendRouteOnSameTransport_When_SendHandlerRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b =>
        {
            b.Services.AddSingleton(new MessageRecorder());
            b.AddRequestHandler<TestOneWayHandler>();
        });
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act
        var inboundRoute = runtime.Router.InboundRoutes.Single(r =>
            r.Kind == InboundRouteKind.Send && r.Endpoint?.Transport == transport);
        var outboundRoute = runtime.Router.OutboundRoutes.Single(r =>
            r.Kind == OutboundRouteKind.Send && r.Endpoint?.Transport == transport);

        // assert
        Assert.Same(inboundRoute.Endpoint!.Transport, outboundRoute.Endpoint!.Transport);
    }

    [Fact]
    public async Task SendAsync_Should_Throw_When_ReplyEndpointTransportDoesNotSupportRequestReply()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddRequestHandler<TestOneWayHandler>();
        builder.AddInMemory();
        builder.ConfigureMessageBus(b => b.AddTransport(new NoReplyTransport()));

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await bus.SendAsync(
                new TestOneWayCommand { Id = "cmd-1" },
                new SendOptions { ReplyEndpoint = new Uri("noreply:///reply") },
                CancellationToken.None));

        // assert
        Assert.Contains("ReplyEndpoint", ex.Message);
        Assert.Contains("no-reply", ex.Message);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    public sealed class TestEvent
    {
        public string Data { get; init; } = "";
    }

    public sealed class TestEventHandler : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent @event, CancellationToken cancellationToken) => default;
    }

    public sealed class TestRequest : IEventRequest<TestResponse>
    {
        public required string Id { get; init; }
    }

    public sealed class TestResponse
    {
        public required string Id { get; init; }
    }

    public sealed class TestRequestHandler : IEventRequestHandler<TestRequest, TestResponse>
    {
        public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
            => new(new TestResponse { Id = request.Id });
    }

    public sealed class TestOneWayCommand
    {
        public required string Id { get; init; }
    }

    public sealed class TestOneWayHandler(MessageRecorder recorder) : IEventRequestHandler<TestOneWayCommand>
    {
        public ValueTask HandleAsync(TestOneWayCommand request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    private sealed class NoReplyTransport : MessagingTransport
    {
        private MessagingTopology? _topology;

        public override MessagingTransportCapabilities Capabilities => MessagingTransportCapabilities.None;

        public override MessagingTopology Topology => _topology!;

        protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
            => new NoReplyTransportConfiguration
            {
                Name = "no-reply",
                Schema = "noreply"
            };

        protected override void OnAfterInitialized(IMessagingSetupContext context)
        {
            _topology = new NoReplyTopology(this);
        }

        public override bool TryGetDispatchEndpoint(
            Uri address,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out DispatchEndpoint? endpoint)
        {
            endpoint = null;
            return false;
        }

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            OutboundRoute route)
            => null;

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            Uri address)
            => null;

        public override ReceiveEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            InboundRoute route)
            => null;

        protected override ReceiveEndpoint CreateReceiveEndpoint()
            => throw new NotSupportedException();

        protected override DispatchEndpoint CreateDispatchEndpoint()
            => throw new NotSupportedException();
    }

    private sealed class NoReplyTransportConfiguration : MessagingTransportConfiguration;

    private sealed class NoReplyTopology(MessagingTransport transport)
        : MessagingTopology(transport, new Uri("noreply:///"));
}
