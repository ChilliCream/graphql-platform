using System.Text.Json;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Infrastructure;

public class MessagePreparationTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("Serialization")]
    [InlineData("Scheduling")]
    [InlineData("Outbox")]
    public async Task PublishAsync_Should_SerializeMiddlewareChanges_When_MiddlewareIsRegisteredAfterTerminal(string after)
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
                    "ChangeMessage"),
                after: after)));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TargetMessage("persisted"), CancellationToken.None);

        // assert
        var envelope = Assert.Single(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        new
        {
            MiddlewareCalls = middlewareCalls,
            DurableHeader = envelope.Headers!.GetValue("durable-header"),
            Message = Deserialize(envelope).Value,
            TransportCalls = provider.GetRequiredService<TransportCounter>().Calls
        }.MatchInlineSnapshot(
            """
            {
              "MiddlewareCalls": 1,
              "DurableHeader": "prepared-once",
              "Message": "from-middleware",
              "TransportCalls": 0
            }
            """);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PrepareAsync_Should_UseCallerScopeAndCorrelation_When_MessageIsPrepared(bool send, bool outbox)
    {
        // arrange
        await using var provider = await CreateProviderAsync(outbox, builder =>
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
        var envelope = await CaptureAsync(bus, new TargetMessage("prepared"), send);

        // assert
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
              "Message": "prepared",
              "DefaultOutboxMessages": 0
            }
            """);
        Assert.Equal(send ? MessageKind.Send : MessageKind.Publish, envelope.Headers!.Get(MessageHeaders.MessageKind));
    }

    [Theory]
    [InlineData(false, "Serialization")]
    [InlineData(false, "Scheduling")]
    [InlineData(false, "Outbox")]
    [InlineData(true, "Serialization")]
    [InlineData(true, "Scheduling")]
    [InlineData(true, "Outbox")]
    public async Task PrepareAsync_Should_SerializeFinalState_When_MiddlewareRunsAfterTerminal(bool send, string after)
    {
        // arrange
        var time = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) => async context =>
                    {
                        await next(context);
                        context.Message = new TargetMessage("after-next");
                        context.CorrelationId = "final-correlation";
                        context.SentAt = time;
                        context.DeliverBy = time.AddMinutes(5);
                        context.Headers.Set("final-header", "after-next");
                    },
                    "MutateAfterNext"),
                after: after)));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var envelope = await CaptureAsync(bus, new TargetMessage("before-next"), send);

        // assert
        new
        {
            Message = Deserialize(envelope).Value,
            envelope.CorrelationId,
            envelope.SentAt,
            envelope.DeliverBy,
            Header = envelope.Headers!.GetValue("final-header"),
            DefaultOutboxMessages = provider.GetRequiredService<CapturingOutbox>().Envelopes.Count,
            TransportCalls = provider.GetRequiredService<TransportCounter>().Calls
        }.MatchInlineSnapshot(
            """
            {
              "Message": "after-next",
              "CorrelationId": "final-correlation",
              "SentAt": "2030-01-01T00:00:00+00:00",
              "DeliverBy": "2030-01-01T00:05:00+00:00",
              "Header": "after-next",
              "DefaultOutboxMessages": 0,
              "TransportCalls": 0
            }
            """);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PrepareAsync_Should_ReturnContext_When_MiddlewareFails(bool send, bool afterNext)
    {
        // arrange
        var pool = new TrackingContextPool();
        var fail = true;
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
        {
            builder.Services.AddSingleton<ObjectPool<DispatchContext>>(pool);
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) => async context =>
                    {
                        if (afterNext || !fail)
                        {
                            await next(context);
                        }

                        if (fail)
                        {
                            throw new InvalidOperationException("preparation failed");
                        }
                    },
                    "Failure"),
                before: "Serialization"));
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await PrepareAsync(bus, new TargetMessage("failed"), send, static (_, _) => { }));
        fail = false;
        await bus.PublishAsync(new TargetMessage("ordinary"), CancellationToken.None);

        // assert
        Assert.Equal("preparation failed", exception.Message);
        var ordinary = Assert.Single(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal("ordinary", Deserialize(ordinary).Value);
        Assert.Equal((1, 2), (pool.ContextCount, pool.ReturnCount));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrepareAsync_Should_ReturnContext_When_CallbackThrows(bool send)
    {
        // arrange
        var pool = new TrackingContextPool();
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.Services.AddSingleton<ObjectPool<DispatchContext>>(pool));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        string? callbackMessage = null;
        var returnedDuringCallback = -1;

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await PrepareAsync(bus, new TargetMessage("callback"), send, (envelope, _) =>
            {
                callbackMessage = Deserialize(envelope).Value;
                returnedDuringCallback = pool.ReturnCount;
                throw new InvalidOperationException("callback failed");
            }));
        var next = await CaptureAsync(bus, new TargetMessage("reused"), send);

        // assert
        new
        {
            Error = exception.Message,
            CallbackMessage = callbackMessage,
            ReturnedDuringCallback = returnedDuringCallback,
            NextMessage = Deserialize(next).Value,
            pool.ContextCount,
            pool.ReturnCount
        }.MatchInlineSnapshot(
            """
            {
              "Error": "callback failed",
              "CallbackMessage": "callback",
              "ReturnedDuringCallback": 0,
              "NextMessage": "reused",
              "ContextCount": 1,
              "ReturnCount": 2
            }
            """);
    }

    [Fact]
    public async Task PrepareAsync_Should_PreserveOwnedEnvelopes_When_ContextIsReused()
    {
        // arrange
        var pool = new TrackingContextPool();
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.Services.AddSingleton<ObjectPool<DispatchContext>>(pool));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var first = await CaptureAsync(bus, new TargetMessage("first"), send: false);
        var second = await CaptureAsync(bus, new TargetMessage("other"), send: true);

        // assert
        new
        {
            Messages = new[] { Deserialize(first).Value, Deserialize(second).Value },
            MessageKinds = new[]
            {
                first.Headers!.Get(MessageHeaders.MessageKind),
                second.Headers!.Get(MessageHeaders.MessageKind)
            },
            pool.ContextCount,
            pool.ReturnCount
        }.MatchInlineSnapshot(
            """
            {
              "Messages": [
                "first",
                "other"
              ],
              "MessageKinds": [
                "publish",
                "send"
              ],
              "ContextCount": 1,
              "ReturnCount": 2
            }
            """);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrepareAsync_Should_RejectShortCircuit_When_MiddlewareDoesNotComplete(bool send)
    {
        // arrange
        var pool = new TrackingContextPool();
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
        {
            builder.Services.AddSingleton<ObjectPool<DispatchContext>>(pool);
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(static (_, _) => static _ => default, "ShortCircuit")));
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await PrepareAsync(bus, new TargetMessage("incomplete"), send, static (_, _) => { }));

        // assert
        Assert.Equal("The message preparation pipeline must complete before a message can be staged.", exception.Message);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(1, pool.ReturnCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PrepareAsync_Should_ReturnContext_When_Cancelled(bool send, bool afterNext)
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        var pool = new TrackingContextPool();
        var entered = false;
        var callbackCalled = false;
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
        {
            builder.Services.AddSingleton<ObjectPool<DispatchContext>>(pool);
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) => async context =>
                    {
                        entered = true;
                        await next(context);
                        cancellation.Cancel();
                    },
                    "CancelAfterNext")));
        });
        using var scope = provider.CreateScope();
        var bus = (IMessageDispatcher)scope.ServiceProvider.GetRequiredService<IMessageBus>();
        if (!afterNext)
        {
            cancellation.Cancel();
        }

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await (send
                ? bus.PrepareSendAsync(new TargetMessage("cancelled"), SendOptions.Default,
                    (_, _) => callbackCalled = true, cancellation.Token)
                : bus.PreparePublishAsync(new TargetMessage("cancelled"), PublishOptions.Default,
                    (_, _) => callbackCalled = true, cancellation.Token));
        });

        // assert
        Assert.Equal(afterNext, entered);
        Assert.False(callbackCalled);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(1, pool.ReturnCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PrepareAsync_Should_RejectScheduledMessage_When_TimeIsSpecified(bool send, bool scheduledByMiddleware)
    {
        // arrange
        var store = new CapturingScheduledMessageStore();
        var scheduledTime = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
        {
            builder.Services.AddSingleton(store);
            builder.Services.AddSingleton(new ScheduledMessageStoreRegistration(
                typeof(InMemoryMessagingTransport), "test:", typeof(CapturingScheduledMessageStore)));
            if (scheduledByMiddleware)
            {
                builder.ConfigureMessageBus(b => b.UseDispatch(
                    new DispatchMiddlewareConfiguration(
                        (_, next) => async context =>
                        {
                            await next(context);
                            context.ScheduledTime = scheduledTime;
                        },
                        "ScheduleMessage"),
                    before: "Scheduling"));
            }
        });
        using var scope = provider.CreateScope();
        var dispatcher = (IMessageDispatcher)scope.ServiceProvider.GetRequiredService<IMessageBus>();
        DateTimeOffset? optionsScheduledTime = scheduledByMiddleware ? null : scheduledTime;

        // act
        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await (send
                ? dispatcher.PrepareSendAsync(new TargetMessage("scheduled"),
                    new SendOptions { ScheduledTime = optionsScheduledTime }, static (_, _) => { }, CancellationToken.None)
                : dispatcher.PreparePublishAsync(new TargetMessage("scheduled"),
                    new PublishOptions { ScheduledTime = optionsScheduledTime }, static (_, _) => { }, CancellationToken.None));
        });

        // assert
        Assert.Equal("Scheduled messages cannot be prepared for an outbox.", exception.Message);
        Assert.Empty(store.Envelopes);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
    }

    [Fact]
    public async Task PrepareAsync_Should_KeepMessagesIsolated_When_PreparationsOverlap()
    {
        // arrange
        var bothEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = 0;
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
        var publish = CaptureAsync(bus, new TargetMessage("published"), send: false);
        var send = CaptureAsync(bus, new TargetMessage("sent"), send: true);
        var messages = await Task.WhenAll(publish.AsTask(), send.AsTask());

        // assert
        Assert.Equal("published", Deserialize(messages[0]).Value);
        Assert.Equal("sent", Deserialize(messages[1]).Value);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
    }

#if DEBUG
    [Fact(Skip = "Allocation checks require a Release build.")]
#else
    [Fact]
#endif
    public async Task PrepareAsync_Should_PreserveAllocationBudget_When_MessageIsPrepared()
    {
        // arrange
        await using var plainProvider = await CreateProviderAsync(outbox: false, static _ => { });
        await using var outboxProvider = await CreateProviderAsync(outbox: true, static _ => { });
        using var plainScope = plainProvider.CreateScope();
        using var outboxScope = outboxProvider.CreateScope();
        var plainBus = plainScope.ServiceProvider.GetRequiredService<IMessageBus>();
        var outboxBus = outboxScope.ServiceProvider.GetRequiredService<IMessageBus>();
        var message = new TargetMessage("allocation-probe");

        // act
        var plainBytes = MeasureAllocations(plainBus, message, prepare: false);
        var preparationBytes = MeasureAllocations(outboxBus, message, prepare: true);
        output.WriteLine($"Warmed publish: plain {plainBytes / 4096d} B/op; prepare {preparationBytes / 4096d} B/op.");

        // assert
        Assert.InRange(preparationBytes, 0, plainBytes);
        Assert.Empty(outboxProvider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(16384, plainProvider.GetRequiredService<TransportCounter>().Calls);
        Assert.Equal(0, outboxProvider.GetRequiredService<TransportCounter>().Calls);
    }

    private static long MeasureAllocations(IMessageBus bus, TargetMessage message, bool prepare)
    {
        for (var i = 0; i < 4096; i++)
        {
            Execute(bus, message, prepare);
        }

        var minimum = long.MaxValue;
        for (var sample = 0; sample < 3; sample++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 4096; i++)
            {
                Execute(bus, message, prepare);
            }

            minimum = Math.Min(minimum, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        return minimum;
    }

    private static void Execute(IMessageBus bus, TargetMessage message, bool prepare)
    {
        if (prepare)
        {
            PrepareAsync(bus, message, send: false, static (_, _) => { }).GetAwaiter().GetResult();
        }
        else
        {
            bus.PublishAsync(message, CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    private static async ValueTask<MessageEnvelope> CaptureAsync(IMessageBus bus, TargetMessage message, bool send)
    {
        MessageEnvelope envelope = null!;
        await PrepareAsync(bus, message, send, (prepared, _) => envelope = MessageEnvelope.CopyWithOwnedBody(prepared));
        return envelope;
    }

    private static ValueTask PrepareAsync(
        IMessageBus bus,
        TargetMessage message,
        bool send,
        Action<MessageEnvelope, CancellationToken> onPrepared)
        => send
            ? ((IMessageDispatcher)bus).PrepareSendAsync(message, SendOptions.Default, onPrepared, CancellationToken.None)
            : ((IMessageDispatcher)bus).PreparePublishAsync(message, PublishOptions.Default, onPrepared, CancellationToken.None);

    private static TargetMessage Deserialize(MessageEnvelope envelope)
        => JsonSerializer.Deserialize<TargetMessage>(envelope.Body.Span, JsonSerializerOptions.Web)!;

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
        builder.AddRequestHandler<TargetMessageHandler>();
        if (outbox)
        {
            builder.UseOutboxCore();
        }

        builder.ConfigureMessageBus(b =>
        {
            b.AddTransport(new InMemoryMessagingTransport(static _ => { }));
            b.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, _) =>
                    {
                        return _ =>
                        {
                            counter.Calls++;
                            return default;
                        };
                    },
                    "TestTerminal")
                {
                    RunsDuringPreparation = false,
                    IsTerminal = true
                },
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

    private sealed class TransportCounter
    {
        public int Calls { get; set; }
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
