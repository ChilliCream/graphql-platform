using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Infrastructure;

public class MessagePreparationTests
{
    private static readonly JsonSerializerOptions s_webOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PublishAsync_Should_PersistMiddlewareChanges_When_ApplicationMiddlewareRuns()
    {
        // arrange
        var middlewareCalls = 0;
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) => context =>
                    {
                        middlewareCalls++;
                        context.Message = new TargetMessage("from-middleware");
                        context.Headers.Set("durable-header", "prepared-once");
                        return next(context);
                    },
                    "ChangeMessage"))));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TargetMessage("persisted"), CancellationToken.None);

        // assert
        var envelope = Assert.Single(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(1, middlewareCalls);
        Assert.Equal("prepared-once", envelope.Headers!.GetValue("durable-header"));
        Assert.Equal("from-middleware", Deserialize(envelope).Value);
        Assert.Equal(0, provider.GetRequiredService<TransportCounter>().Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublishAsync_Should_FinalizeBeforeMiddlewareUnwinds_When_MessageIsDispatched(bool outbox)
    {
        // arrange
        string? observed = null;
        await using var provider = await CreateProviderAsync(outbox, builder =>
            builder.ConfigureMessageBus(bus => bus.UseDispatch(new DispatchMiddlewareConfiguration(
                (_, next) => async context =>
                {
                    await next(context);
                    var envelopes = outbox
                        ? context.Services.GetRequiredService<CapturingOutbox>().Envelopes
                        : context.Services.GetRequiredService<TransportCounter>().Envelopes;
                    observed = Deserialize(envelopes.Single()).Value;
                    context.Message = new TargetMessage("after-next");
                },
                "ObserveFinalization"))));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TargetMessage("before-next"), CancellationToken.None);

        // assert
        var envelopes = outbox
            ? provider.GetRequiredService<CapturingOutbox>().Envelopes
            : provider.GetRequiredService<TransportCounter>().Envelopes;
        Assert.Equal("before-next", observed);
        Assert.Equal(["before-next"], envelopes.Select(envelope => Deserialize(envelope).Value));
    }

    [Theory]
    [InlineData("Instrumentation")]
    [InlineData("Serialization")]
    [InlineData("Scheduling")]
    [InlineData("Outbox")]
    public async Task Configure_Should_AcceptAnchor_When_MiddlewareReferencesBuiltInStage(string key)
    {
        // arrange
        var middleware = new DispatchMiddlewareConfiguration(
            static (_, next) => context =>
            {
                context.Headers.Set("x-anchor", "set");
                return next(context);
            },
            "ApplicationMiddleware");

        // act
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(bus => bus.UseDispatch(middleware, before: key)));
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(new TargetMessage("anchored"), CancellationToken.None);

        // assert
        var envelope = Assert.Single(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal("anchored", Deserialize(envelope).Value);
        Assert.Equal("set", envelope.Headers!.GetValue("x-anchor"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task PublishAsync_Should_RunSerializationAnchoredMiddleware_When_MessageIsStored(
        bool after,
        bool scheduled)
    {
        // arrange
        var calls = 0;
        var store = new CapturingScheduledMessageStore();
        var middleware = new DispatchMiddlewareConfiguration(
            (_, next) => context =>
            {
                calls++;
                context.Headers.Set("x-anchor", "set");
                return next(context);
            },
            "SerializationAnchored");
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
        {
            builder.Services.RemoveAll<ScheduledMessageStoreRegistration>();
            builder.Services.AddSingleton(store);
            builder.Services.AddSingleton(new ScheduledMessageStoreRegistration(
                typeof(InMemoryMessagingTransport), "test:", typeof(CapturingScheduledMessageStore)));
            builder.ConfigureMessageBus(bus =>
            {
                if (after)
                {
                    bus.UseDispatch(middleware, after: "Serialization");
                }
                else
                {
                    bus.UseDispatch(middleware, before: "Serialization");
                }
            });
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        if (scheduled)
        {
            await bus.SchedulePublishAsync(
                new TargetMessage("anchored"),
                new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
                CancellationToken.None);
        }
        else
        {
            await bus.PublishAsync(new TargetMessage("anchored"), CancellationToken.None);
        }

        // assert
        var envelope = scheduled
            ? Assert.Single(store.Envelopes)
            : Assert.Single(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(1, calls);
        Assert.Equal("set", envelope.Headers!.GetValue("x-anchor"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublishAsync_Should_Complete_When_MiddlewareCancelsAfterDispatch(bool outbox)
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        await using var provider = await CreateProviderAsync(outbox, builder =>
            builder.ConfigureMessageBus(bus => bus.UseDispatch(new DispatchMiddlewareConfiguration(
                (_, next) => async context =>
                {
                    await next(context);
                    cancellation.Cancel();
                },
                "CancelAfterDispatch"))));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TargetMessage("dispatched"), cancellation.Token);

        // assert
        var envelopes = outbox
            ? provider.GetRequiredService<CapturingOutbox>().Envelopes
            : provider.GetRequiredService<TransportCounter>().Envelopes;
        Assert.Equal("dispatched", Deserialize(Assert.Single(envelopes)).Value);
        Assert.True(cancellation.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScheduleAsync_Should_ReturnRequestedTime_When_MiddlewareChangesTimeAfterNext(bool send)
    {
        // arrange
        var time = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var store = new CapturingScheduledMessageStore();
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
        {
            builder.Services.RemoveAll<ScheduledMessageStoreRegistration>();
            builder.Services.AddSingleton(store);
            builder.Services.AddSingleton(new ScheduledMessageStoreRegistration(
                typeof(InMemoryMessagingTransport), "test:", typeof(CapturingScheduledMessageStore)));
            builder.ConfigureMessageBus(bus => bus.UseDispatch(new DispatchMiddlewareConfiguration(
                (_, next) => async context =>
                {
                    await next(context);
                    context.ScheduledTime = time.AddDays(1);
                },
                "ChangeScheduleAfterNext")));
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var result = await (send
            ? bus.ScheduleSendAsync(new TargetMessage("scheduled"), time, CancellationToken.None)
            : bus.SchedulePublishAsync(new TargetMessage("scheduled"), time, CancellationToken.None));

        // assert
        Assert.Equal(
            new SchedulingResult { Token = "test:scheduled", ScheduledTime = time, IsCancellable = true },
            result);
        Assert.Equal(
            [(time, "scheduled")],
            store.Envelopes.Select(envelope => (envelope.ScheduledTime, Deserialize(envelope).Value)));
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(0, provider.GetRequiredService<TransportCounter>().Calls);
    }

    private static TargetMessage Deserialize(MessageEnvelope envelope)
        => JsonSerializer.Deserialize<TargetMessage>(envelope.Body.Span, s_webOptions)!;

    private static async Task<ServiceProvider> CreateProviderAsync(
        bool outbox,
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var counter = new TransportCounter();
        services.AddSingleton<CapturingOutbox>();
        services.AddSingleton(counter);
        services.AddSingleton<IMessageOutbox>(sp => sp.GetRequiredService<CapturingOutbox>());
        var builder = services.AddMessageBus();
        if (outbox)
        {
            builder.UseOutboxCore();
        }

        builder.AddInMemory(transport => transport.DeclareQueue("target"));
        configure(builder);
        var provider = services.BuildServiceProvider();
        try
        {
            var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
            await runtime.StartAsync(CancellationToken.None);
            var transport = runtime.Transports.Single();
            var endpoint = new CapturingDispatchEndpoint(transport, counter);
            endpoint.Initialize(runtime, new DispatchEndpointConfiguration { Name = "target" });
            endpoint.Complete(runtime);
            foreach (var kind in new[] { OutboundRouteKind.Publish, OutboundRouteKind.Send })
            {
                var route = new OutboundRoute();
                route.Initialize(runtime, new OutboundRouteConfiguration
                {
                    RuntimeType = typeof(TargetMessage),
                    Kind = kind
                });
                route.ConnectEndpoint(runtime, endpoint);
                route.Complete(runtime);
            }

            return provider;
        }
        catch
        {
            await provider.DisposeAsync();
            throw;
        }
    }

    public sealed record TargetMessage(string Value) : IEventRequest;

    private sealed class CapturingDispatchEndpoint(MessagingTransport transport, TransportCounter counter)
        : DispatchEndpoint(transport)
    {
        protected override void OnInitialize(
            IMessagingConfigurationContext context,
            DispatchEndpointConfiguration configuration)
        { }

        protected override void OnComplete(
            IMessagingConfigurationContext context,
            DispatchEndpointConfiguration configuration)
        {
            Destination = ((InMemoryMessagingTopology)Transport.Topology).GetQueue("target")!;
            Address = Destination.Address;
        }

        protected override ValueTask DispatchAsync(IDispatchContext context)
        {
            counter.Calls++;
            counter.Envelopes.Add(MessageEnvelope.CopyWithOwnedBody(context.Envelope!));
            return default;
        }
    }

    private sealed class ScopedState
    {
        public string Value { get; set; } = "unassigned";
    }

    private sealed class RecordingDiagnostics : MessagingDiagnosticEventListener
    {
        public List<string> Events { get; } = [];

        public override IDisposable Dispatch(IDispatchContext context)
        {
            Events.Add("begin");
            return new RecordingScope(Events);
        }

        public override void DispatchError(IDispatchContext context, Exception exception)
            => Events.Add("error");

        private sealed class RecordingScope(List<string> events) : IDisposable
        {
            public void Dispose() => events.Add("end");
        }
    }

    private sealed class TransportCounter
    {
        public int Calls { get; set; }

        public List<MessageEnvelope> Envelopes { get; } = [];
    }

    private sealed class CapturingOutbox : IMessageOutbox
    {
        public List<MessageEnvelope> Envelopes { get; } = [];

        public Exception? Failure { get; set; }

        public Action? OnPersist { get; set; }

        public ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            if (Failure is { } failure)
            {
                throw failure;
            }

            Envelopes.Add(MessageEnvelope.CopyWithOwnedBody(envelope));
            OnPersist?.Invoke();
            return default;
        }
    }

    private sealed class CapturingScheduledMessageStore : IScheduledMessageStore
    {
        public List<MessageEnvelope> Envelopes { get; } = [];

        public ValueTask<string> PersistAsync(IDispatchContext context, CancellationToken cancellationToken)
        {
            Envelopes.Add(MessageEnvelope.CopyWithOwnedBody(context.Envelope!));
            return ValueTask.FromResult("test:scheduled");
        }

        public ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
            => ValueTask.FromResult(false);
    }
}
