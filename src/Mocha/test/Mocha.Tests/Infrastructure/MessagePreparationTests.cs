using System.Text.Json;
using CookieCrumble;
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
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task PublishAsync_Should_AllowMiddlewareToCatchFailure_When_FinalizationFails(bool serialization, bool explicitOutbox)
    {
        // arrange
        Exception? caught = null;
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(bus => bus.UseDispatch(new DispatchMiddlewareConfiguration(
                (_, next) => async context =>
                {
                    if (serialization)
                    {
                        context.ContentType = new MessageContentType("application/unsupported");
                    }

                    try
                    {
                        await next(context);
                    }
                    catch (InvalidOperationException exception)
                    {
                        caught = exception;
                    }
                },
                "CatchFinalizationFailure"))));
        var outbox = provider.GetRequiredService<CapturingOutbox>();
        outbox.Failure = new InvalidOperationException("Persistence failed.");
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        if (explicitOutbox)
        {
            await DispatchToOutboxAsync(bus, new TargetMessage("failed"), send: false,
                static (_, _) => throw new InvalidOperationException("Persistence failed."));
        }
        else
        {
            await bus.PublishAsync(new TargetMessage("failed"), CancellationToken.None);
        }

        // assert
        Assert.IsType<InvalidOperationException>(caught);
        Assert.Empty(outbox.Envelopes);
        Assert.Equal(0, provider.GetRequiredService<TransportCounter>().Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_Should_PreserveConfiguredOrder_When_MiddlewareUsesBeforeAndAfter(bool prepare)
    {
        // arrange
        var events = new List<string>();
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(bus =>
            {
                bus.UseDispatch(CreateMiddleware("middle"));
                bus.UseDispatch(CreateMiddleware("last"), after: "middle");
                bus.UseDispatch(CreateMiddleware("first"), before: "middle");
            }));
        provider.GetRequiredService<CapturingOutbox>().OnPersist = () => events.Add("persist");
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        if (prepare)
        {
            await DispatchToOutboxAsync(bus, new TargetMessage("prepared"), send: false, (_, _) => events.Add("stage"));
        }
        else
        {
            await bus.PublishAsync(new TargetMessage("published"), CancellationToken.None);
        }

        // assert
        if (prepare)
        {
            events.MatchInlineSnapshot(
                """
                [
                  "first:before",
                  "middle:before",
                  "last:before",
                  "stage",
                  "last:after",
                  "middle:after",
                  "first:after"
                ]
                """);
        }
        else
        {
            events.MatchInlineSnapshot(
                """
                [
                  "first:before",
                  "middle:before",
                  "last:before",
                  "persist",
                  "last:after",
                  "middle:after",
                  "first:after"
                ]
                """);
        }

        DispatchMiddlewareConfiguration CreateMiddleware(string name)
            => new((_, next) => async context =>
            {
                events.Add($"{name}:before");
                await next(context);
                events.Add($"{name}:after");
            }, name);
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
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task DispatchAsync_Should_ReportOneDiagnosticScope_When_SerializationSucceedsOrFails(
        bool prepare,
        bool failSerialization)
    {
        // arrange
        var diagnostics = new RecordingDiagnostics();
        var callbackCalled = false;
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
        {
            builder.Services.AddSingleton<IMessagingDiagnosticEventListener>(diagnostics);
            builder.ConfigureMessageBus(bus => bus.UseDispatch(new DispatchMiddlewareConfiguration(
                (_, next) => context =>
                {
                    if (failSerialization)
                    {
                        context.ContentType = new MessageContentType("application/unsupported");
                    }

                    return next(context);
                },
                "ChooseSerializer")));
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        if (failSerialization)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(DispatchAsync);
        }
        else
        {
            await DispatchAsync();
        }

        // assert
        Assert.Equal(failSerialization ? ["begin", "error", "end"] : ["begin", "end"], diagnostics.Events);
        Assert.Equal(prepare && !failSerialization, callbackCalled);
        Assert.Equal(
            prepare || failSerialization ? 0 : 1,
            provider.GetRequiredService<CapturingOutbox>().Envelopes.Count);

        async Task DispatchAsync()
        {
            if (prepare)
            {
                await DispatchToOutboxAsync(
                    bus, new TargetMessage("prepared"), send: false, (_, _) => callbackCalled = true);
            }
            else
            {
                await bus.PublishAsync(new TargetMessage("published"), CancellationToken.None);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_Should_UseCallerScopeAndCorrelation_When_OutboxIsExplicit(bool send)
    {
        // arrange
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
                    "ScopedHeader")));
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
        Assert.Equal("caller", envelope.Headers!.GetValue("scope"));
        Assert.Equal("incoming-conversation", envelope.ConversationId);
        Assert.Equal("incoming-message", envelope.CausationId);
        Assert.Equal(send ? MessageKind.Send : MessageKind.Publish, envelope.Headers!.Get(MessageHeaders.MessageKind));
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_Should_SerializeAtOutbox_When_MiddlewareChangesMessageAfterNext(bool send)
    {
        // arrange
        var time = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) => async context =>
                    {
                        context.CorrelationId = "initial-correlation";
                        await next(context);
                        context.Message = new TargetMessage("after-next");
                        context.CorrelationId = "final-correlation";
                        context.SentAt = time;
                        context.DeliverBy = time.AddMinutes(5);
                        context.Headers.Set("final-header", "after-next");
                    },
                    "MutateAfterNext"))));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var envelope = await CaptureAsync(bus, new TargetMessage("before-next"), send);

        // assert
        Assert.Equal("before-next", Deserialize(envelope).Value);
        Assert.Equal("initial-correlation", envelope.CorrelationId);
        Assert.Null(envelope.SentAt);
        Assert.Null(envelope.DeliverBy);
        Assert.Null(envelope.Headers!.GetValue("final-header"));
    }

    [Fact]
    public async Task PublishAsync_Should_UseDefaultOutbox_When_PreviousExplicitOutboxDispatchFailed()
    {
        // arrange
        var fail = true;
        var callbackCalled = false;
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) => async context =>
                    {
                        await next(context);

                        if (fail)
                        {
                            throw new InvalidOperationException("preparation failed");
                        }
                    },
                    "Failure"))));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await DispatchToOutboxAsync(
                bus, new TargetMessage("failed"), send: false, (_, _) => callbackCalled = true));
        fail = false;
        await bus.PublishAsync(new TargetMessage("ordinary"), CancellationToken.None);

        // assert
        Assert.Equal("preparation failed", exception.Message);
        Assert.True(callbackCalled);
        Assert.Equal(
            ["ordinary"],
            provider.GetRequiredService<CapturingOutbox>().Envelopes.Select(envelope => Deserialize(envelope).Value));
    }

    [Fact]
    public async Task DispatchAsync_Should_ReportPersistenceFailure_When_ExplicitOutboxThrows()
    {
        // arrange
        var diagnostics = new RecordingDiagnostics();
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.Services.AddSingleton<IMessagingDiagnosticEventListener>(diagnostics));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await DispatchToOutboxAsync(bus, new TargetMessage("failed"), send: false,
                static (_, _) => throw new InvalidOperationException("Persistence failed.")));

        // assert
        Assert.Equal("Persistence failed.", exception.Message);
        diagnostics.Events.MatchInlineSnapshot(
            """
            [
              "begin",
              "error",
              "end"
            ]
            """);
    }

    [Fact]
    public async Task PublishAsync_Should_NeitherStoreNorSend_When_MiddlewareShortCircuits()
    {
        // arrange
        var persisted = false;
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(static (_, _) => static _ => default, "ShortCircuit"))));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await DispatchToOutboxAsync(bus, new TargetMessage("incomplete"), send: false, (_, _) => persisted = true);

        // assert
        Assert.False(persisted);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(0, provider.GetRequiredService<TransportCounter>().Calls);
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

    [Fact]
    public async Task PublishAsync_Should_StageMessage_When_CancelledAfterDispatch()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        var callbackCalled = false;
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) => async context =>
                    {
                        await next(context);
                        cancellation.Cancel();
                    },
                    "CancelAfterNext"))));
        using var scope = provider.CreateScope();
        var bus = (DefaultMessageBus)scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var outbox = new CallbackOutbox((_, _) => callbackCalled = true);

        // act
        await bus.PublishAsync(new TargetMessage("cancelled"), PublishOptions.Default, outbox, cancellation.Token);

        // assert
        Assert.True(callbackCalled);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DispatchAsync_Should_UseExplicitOutbox_When_TimeIsSpecified(bool send, bool scheduledByMiddleware)
    {
        // arrange
        var store = new CapturingScheduledMessageStore();
        var scheduledTime = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        MessageEnvelope? captured = null;
        var target = new CallbackOutbox((envelope, _) => captured = MessageEnvelope.CopyWithOwnedBody(envelope));
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
        {
            builder.Services.RemoveAll<ScheduledMessageStoreRegistration>();
            builder.Services.AddSingleton(store);
            builder.Services.AddSingleton(new ScheduledMessageStoreRegistration(
                typeof(InMemoryMessagingTransport), "test:", typeof(CapturingScheduledMessageStore)));
            if (scheduledByMiddleware)
            {
                builder.ConfigureMessageBus(b => b.UseDispatch(
                    new DispatchMiddlewareConfiguration(
                        (_, next) => context =>
                        {
                            context.ScheduledTime = scheduledTime;
                            return next(context);
                        },
                        "ScheduleMessage")));
            }
        });
        using var scope = provider.CreateScope();
        var dispatcher = (DefaultMessageBus)scope.ServiceProvider.GetRequiredService<IMessageBus>();
        DateTimeOffset? optionsScheduledTime = scheduledByMiddleware ? null : scheduledTime;

        // act
        await (send
            ? dispatcher.SendAsync(new TargetMessage("scheduled"),
                new SendOptions { ScheduledTime = optionsScheduledTime }, target, CancellationToken.None)
            : dispatcher.PublishAsync(new TargetMessage("scheduled"),
                new PublishOptions { ScheduledTime = optionsScheduledTime }, target, CancellationToken.None));

        // assert
        Assert.Equal(scheduledTime, captured!.ScheduledTime);
        Assert.Equal("scheduled", Deserialize(captured).Value);
        Assert.Empty(store.Envelopes);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(0, provider.GetRequiredService<TransportCounter>().Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_Should_PreserveScheduledTime_When_MiddlewareChangesTimeAfterNext(bool send)
    {
        // arrange
        var time = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(bus => bus.UseDispatch(new DispatchMiddlewareConfiguration(
                (_, next) => async context =>
                {
                    await next(context);
                    context.ScheduledTime = time.AddDays(1);
                },
                "ChangeScheduleAfterNext"))));
        using var scope = provider.CreateScope();
        var dispatcher = (DefaultMessageBus)scope.ServiceProvider.GetRequiredService<IMessageBus>();
        MessageEnvelope? captured = null;
        var outbox = new CallbackOutbox((envelope, _) => captured = MessageEnvelope.CopyWithOwnedBody(envelope));

        // act
        await (send
            ? dispatcher.SendAsync(new TargetMessage("scheduled"),
                new SendOptions { ScheduledTime = time }, outbox, CancellationToken.None)
            : dispatcher.PublishAsync(new TargetMessage("scheduled"),
                new PublishOptions { ScheduledTime = time }, outbox, CancellationToken.None));

        // assert
        Assert.Equal("scheduled", Deserialize(captured!).Value);
        Assert.Equal(time, captured!.ScheduledTime);
        Assert.Null(captured.SentAt);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(0, provider.GetRequiredService<TransportCounter>().Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_Should_RejectBeforeStaging_When_NoSchedulerMatchesTransport(bool send)
    {
        // arrange
        var time = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await using var provider = await CreateProviderAsync(outbox: true,
            builder => builder.Services.RemoveAll<ScheduledMessageStoreRegistration>());
        using var scope = provider.CreateScope();
        var dispatcher = (DefaultMessageBus)scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var staged = false;
        var outbox = new CallbackOutbox((_, _) => staged = true);

        // act
        await Assert.ThrowsAsync<NotSupportedException>(async () => await (send
            ? dispatcher.SendAsync(new TargetMessage("scheduled"),
                new SendOptions { ScheduledTime = time }, outbox, CancellationToken.None)
            : dispatcher.PublishAsync(new TargetMessage("scheduled"),
                new PublishOptions { ScheduledTime = time }, outbox, CancellationToken.None)));

        // assert
        Assert.False(staged);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(0, provider.GetRequiredService<TransportCounter>().Calls);
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

    [Fact]
    public async Task DispatchAsync_Should_KeepMessagesIsolated_When_DispatchesOverlap()
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
                    "Overlap"))));
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_Should_ThrowBeforeTransport_When_PipelineHasNoOutboxMiddleware(bool send)
    {
        // arrange
        await using var provider = await CreateProviderAsync(outbox: false, static _ => { });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var stored = 0;

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await DispatchToOutboxAsync(bus, new TargetMessage("unstored"), send, (_, _) => stored++));

        // assert
        Assert.Equal(
            "The message was not sent. It was dispatched through dbContext.Outbox(bus), but the dispatch "
            + "pipeline of endpoint 'target' has no outbox middleware to store it. Use the message bus "
            + "from the service provider that registers the outbox.",
            exception.Message);
        Assert.Equal(0, stored);
        Assert.Equal(0, provider.GetRequiredService<TransportCounter>().Calls);
    }

    private static async ValueTask<MessageEnvelope> CaptureAsync(IMessageBus bus, TargetMessage message, bool send)
    {
        MessageEnvelope envelope = null!;
        await DispatchToOutboxAsync(
            bus, message, send, (prepared, _) => envelope = MessageEnvelope.CopyWithOwnedBody(prepared));
        return envelope;
    }

    private static ValueTask DispatchToOutboxAsync(
        IMessageBus bus,
        TargetMessage message,
        bool send,
        Action<MessageEnvelope, CancellationToken> onPrepared)
        => send
            ? ((DefaultMessageBus)bus).SendAsync(message, SendOptions.Default, new CallbackOutbox(onPrepared), CancellationToken.None)
            : ((DefaultMessageBus)bus).PublishAsync(message, PublishOptions.Default, new CallbackOutbox(onPrepared), CancellationToken.None);

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

    private sealed class CallbackOutbox : IMessageOutbox
    {
        private readonly Action<MessageEnvelope, CancellationToken> _callback;

        public CallbackOutbox(Action<MessageEnvelope, CancellationToken> callback)
        {
            _callback = callback;
        }

        public ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            _callback(envelope, cancellationToken);
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
