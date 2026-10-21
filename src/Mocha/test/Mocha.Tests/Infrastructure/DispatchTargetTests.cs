using System.Text.Json;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Infrastructure;

public class DispatchTargetTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_Should_RejectNullTarget_When_ExplicitDispatchIsRequested(bool send)
    {
        // arrange
        await using var provider = await CreateProviderAsync(outbox: true, static _ => { });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await DispatchAsync(bus, new TargetMessage("must-not-dispatch"), null!, send));

        // assert
        Assert.Equal("target", exception.ParamName);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_Should_UseCallerScopeAndCorrelation_When_TargetIsExplicit(bool send)
    {
        // arrange
        var target = new CapturingTarget(throwOnCapture: false);
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
        {
            builder.Services.AddScoped<ScopedState>();
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    static (_, next) => async context =>
                    {
                        await Task.Yield();
                        context.Headers.Set("scope", context.Services.GetRequiredService<ScopedState>().Value);
                        await next(context);
                    },
                    "ScopedHeader"),
                before: "Serialization"));
        });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ScopedState>().Value = "caller";
        scope.ServiceProvider.GetRequiredService<ConsumeContextAccessor>().Context = new ReceiveContext
        {
            MessageId = "incoming-message",
            ConversationId = "incoming-conversation"
        };
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await DispatchAsync(bus, new TargetMessage("explicit"), target, send);

        // assert
        var envelope = Assert.Single(target.Envelopes);
        envelope.Headers!.TryGetValue("scope", out var scopeValue);
        new
        {
            Scope = scopeValue,
            envelope.ConversationId,
            envelope.CausationId,
            Message = Deserialize(envelope).Value,
            DefaultOutboxMessages = provider.GetRequiredService<CapturingOutbox>().Envelopes.Count
        }.MatchInlineSnapshot(
            """
            {
              "Scope": "caller",
              "ConversationId": "incoming-conversation",
              "CausationId": "incoming-message",
              "Message": "explicit",
              "DefaultOutboxMessages": 0
            }
            """);
        Assert.Equal(send ? MessageKind.Send : MessageKind.Publish, envelope.Headers!.Get(MessageHeaders.MessageKind));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DispatchAsync_Should_ClearTarget_When_ContextIsReturned(bool send, bool throwOnCapture)
    {
        // arrange
        var pool = new TrackingContextPool();
        var target = new CapturingTarget(throwOnCapture);
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.Services.AddSingleton<ObjectPool<DispatchContext>>(pool));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Record.ExceptionAsync(async () =>
            await DispatchAsync(bus, new TargetMessage("explicit"), target, send));
        var resetTarget = pool.Returned!.Features.Get<DispatchTargetFeature>()?.Target;
        await bus.PublishAsync(new TargetMessage("ordinary"), CancellationToken.None);

        // assert
        var ordinary = Assert.Single(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(throwOnCapture ? "capture failed" : null, exception?.Message);
        Assert.Null(resetTarget);
        Assert.Equal(1, target.CaptureCount);
        new
        {
            OrdinaryMessage = Deserialize(ordinary).Value,
            pool.ContextCount,
            pool.ReturnCount
        }.MatchInlineSnapshot(
            """
            {
              "OrdinaryMessage": "ordinary",
              "ContextCount": 1,
              "ReturnCount": 2
            }
            """);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_Should_CaptureScheduledMessage_When_TargetIsExplicit(bool send)
    {
        // arrange
        var target = new CapturingTarget(throwOnCapture: false);
        await using var provider = await CreateProviderAsync(outbox: true, static _ => { });
        using var scope = provider.CreateScope();
        var dispatcher = (IMessageDispatcher)scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // act
        if (send)
        {
            await dispatcher.SendAsync(
                new TargetMessage("scheduled"),
                new SendOptions { ScheduledTime = scheduledTime },
                target,
                CancellationToken.None);
        }
        else
        {
            await dispatcher.PublishAsync(
                new TargetMessage("scheduled"),
                new PublishOptions { ScheduledTime = scheduledTime },
                target,
                CancellationToken.None);
        }

        // assert
        var envelope = Assert.Single(target.Envelopes);
        Assert.Equal(scheduledTime, envelope.ScheduledTime);
        Assert.Equal("scheduled", Deserialize(envelope).Value);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
    }

    [Fact]
    public async Task DispatchAsync_Should_KeepTargetsIsolated_When_DispatchesOverlap()
    {
        // arrange
        var bothEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = 0;
        var publishTarget = new CapturingTarget(throwOnCapture: false);
        var sendTarget = new CapturingTarget(throwOnCapture: false);
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) => async context =>
                    {
                        if (Interlocked.Increment(ref entered) == 2)
                        {
                            bothEntered.SetResult();
                        }

                        await bothEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), context.CancellationToken);
                        await next(context);
                    },
                    "Overlap"),
                before: "Serialization")));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var publish = DispatchAsync(bus, new TargetMessage("published"), publishTarget, send: false);
        var send = DispatchAsync(bus, new TargetMessage("sent"), sendTarget, send: true);
        await Task.WhenAll(publish.AsTask(), send.AsTask());

        // assert
        Assert.Equal("published", Deserialize(Assert.Single(publishTarget.Envelopes)).Value);
        Assert.Equal("sent", Deserialize(Assert.Single(sendTarget.Envelopes)).Value);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
    }

#if DEBUG
    [Fact(Skip = "Allocation checks require a Release build.")]
#else
    [Fact]
#endif
    public async Task PublishAsync_Should_AllocateEqually_When_OutboxIsRegisteredAndBypassed()
    {
        // arrange
        await using var plainProvider = await CreateProviderAsync(outbox: false, static _ => { });
        await using var outboxProvider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    static (_, next) => context =>
                    {
                        context.SkipOutbox();
                        return next(context);
                    },
                    "BypassOutbox"),
                before: "Outbox")));
        using var plainScope = plainProvider.CreateScope();
        using var outboxScope = outboxProvider.CreateScope();
        var plainBus = plainScope.ServiceProvider.GetRequiredService<IMessageBus>();
        var outboxBus = outboxScope.ServiceProvider.GetRequiredService<IMessageBus>();
        var message = new TargetMessage("allocation-probe");

        // act
        var plainBytes = MeasureAllocations(plainBus, message);
        var outboxBytes = MeasureAllocations(outboxBus, message);
        output.WriteLine($"Warmed publish: plain {plainBytes / 4096d} B/op; bypassed outbox {outboxBytes / 4096d} B/op.");

        // assert
        Assert.Equal(plainBytes, outboxBytes);
        Assert.Empty(outboxProvider.GetRequiredService<CapturingOutbox>().Envelopes);
    }

    private static long MeasureAllocations(IMessageBus bus, TargetMessage message)
    {
        for (var i = 0; i < 4096; i++)
        {
            bus.PublishAsync(message, CancellationToken.None).GetAwaiter().GetResult();
        }

        var minimum = long.MaxValue;
        for (var sample = 0; sample < 3; sample++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 4096; i++)
            {
                bus.PublishAsync(message, CancellationToken.None).GetAwaiter().GetResult();
            }

            minimum = Math.Min(minimum, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        return minimum;
    }

    private static ValueTask DispatchAsync(
        IMessageBus bus,
        TargetMessage message,
        IMessageDispatchTarget target,
        bool send)
        => send
            ? ((IMessageDispatcher)bus).SendAsync(message, SendOptions.Default, target, CancellationToken.None)
            : ((IMessageDispatcher)bus).PublishAsync(message, PublishOptions.Default, target, CancellationToken.None);

    private static TargetMessage Deserialize(MessageEnvelope envelope)
        => JsonSerializer.Deserialize<TargetMessage>(envelope.Body.Span, JsonSerializerOptions.Web)!;

    private static async Task<ServiceProvider> CreateProviderAsync(
        bool outbox,
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<CapturingOutbox>();
        services.AddSingleton<IMessageOutbox>(sp => sp.GetRequiredService<CapturingOutbox>());
        var builder = services.AddMessageBus();
        builder.AddRequestHandler<TargetMessageHandler>();
        if (outbox)
        {
            builder.UseOutboxCore();
        }

        builder.ConfigureMessageBus(b =>
        {
            b.AddTransport(new InMemoryMessagingTransport(static _ => { }));
            b.UseDispatch(
                new DispatchMiddlewareConfiguration(static (_, _) => static _ => default, "TestTerminal"),
                after: outbox ? "Outbox" : "Scheduling");
        });
        configure(builder);
        var provider = services.BuildServiceProvider();
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(CancellationToken.None);
        return provider;
    }

    public sealed record TargetMessage(string Value) : IEventRequest;

    public sealed class TargetMessageHandler : IEventRequestHandler<TargetMessage>
    {
        public ValueTask HandleAsync(TargetMessage message, CancellationToken cancellationToken) => default;
    }

    private sealed class ScopedState
    {
        public string Value { get; set; } = "unassigned";
    }

    private sealed class CapturingTarget(bool throwOnCapture) : IMessageDispatchTarget
    {
        public List<MessageEnvelope> Envelopes { get; } = [];

        public int CaptureCount { get; private set; }

        public void Capture(MessageEnvelope envelope)
        {
            CaptureCount++;
            if (throwOnCapture)
            {
                throw new InvalidOperationException("capture failed");
            }

            Envelopes.Add(MessageEnvelope.CopyWithOwnedBody(envelope));
        }
    }

    private sealed class CapturingOutbox : IMessageOutbox
    {
        public List<MessageEnvelope> Envelopes { get; } = [];

        public ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(MessageEnvelope.CopyWithOwnedBody(envelope));
            return default;
        }
    }

    private sealed class TrackingContextPool : ObjectPool<DispatchContext>
    {
        private readonly DispatchContextPool _inner = new();
        private readonly HashSet<DispatchContext> _contexts = [];

        public int ContextCount => _contexts.Count;

        public int ReturnCount { get; private set; }

        public DispatchContext? Returned { get; private set; }

        public override DispatchContext Get()
        {
            var context = _inner.Get();
            _contexts.Add(context);
            return context;
        }

        public override void Return(DispatchContext obj)
        {
            _inner.Return(obj);
            ReturnCount++;
            Returned = obj;
        }
    }
}
