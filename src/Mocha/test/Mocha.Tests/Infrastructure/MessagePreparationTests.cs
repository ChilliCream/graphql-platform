using System.Text.Json;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Infrastructure;

public class MessagePreparationTests(ITestOutputHelper output)
{
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
        new
        {
            ObservedAfterNext = observed,
            Finalized = envelopes.Select(envelope => Deserialize(envelope).Value)
        }.MatchInlineSnapshot(
            """
            {
              "ObservedAfterNext": "before-next",
              "Finalized": [
                "before-next"
              ]
            }
            """);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
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
            await PrepareAsync(bus, new TargetMessage("failed"), send: false,
                static (_, _) => throw new InvalidOperationException("Persistence failed."));
        }
        else
        {
            await bus.PublishAsync(new TargetMessage("failed"), CancellationToken.None);
        }

        // assert
        new
        {
            Caught = caught?.GetType().Name,
            Persisted = outbox.Envelopes.Count,
            TransportCalls = provider.GetRequiredService<TransportCounter>().Calls
        }.MatchInlineSnapshot(
            """
            {
              "Caught": "InvalidOperationException",
              "Persisted": 0,
              "TransportCalls": 0
            }
            """);
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
            await PrepareAsync(bus, new TargetMessage("prepared"), send: false, (_, _) => events.Add("stage"));
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
    [InlineData("Instrumentation", false)]
    [InlineData("Instrumentation", true)]
    [InlineData("Serialization", false)]
    [InlineData("Serialization", true)]
    [InlineData("Scheduling", false)]
    [InlineData("Scheduling", true)]
    [InlineData("Outbox", false)]
    [InlineData("Outbox", true)]
    public async Task Configure_Should_AcceptAnchor_When_MiddlewareReferencesBuiltInStage(string key, bool before)
    {
        // arrange
        var middleware = new DispatchMiddlewareConfiguration(static (_, next) => next, "ApplicationMiddleware");

        // act
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(bus => bus.UseDispatch(
                middleware,
                before: before ? key : null,
                after: before ? null : key)));
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(new TargetMessage("anchored"), CancellationToken.None);

        // assert
        var envelope = Assert.Single(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal("anchored", Deserialize(envelope).Value);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
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
                await PrepareAsync(bus, new TargetMessage("prepared"), send: false, (_, _) => callbackCalled = true);
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
    public async Task PrepareAsync_Should_UseCallerScopeAndCorrelation_When_MessageIsPrepared(bool send)
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
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrepareAsync_Should_SerializeAtOutbox_When_MiddlewareChangesMessageAfterNext(bool send)
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
              "Message": "before-next",
              "CorrelationId": "initial-correlation",
              "SentAt": null,
              "DeliverBy": null,
              "Header": null,
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
        var callbackCalled = false;
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
                    "Failure")));
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await PrepareAsync(bus, new TargetMessage("failed"), send, (_, _) => callbackCalled = true));
        fail = false;
        await bus.PublishAsync(new TargetMessage("ordinary"), CancellationToken.None);

        // assert
        new
        {
            Error = exception.Message,
            CallbackMatchedBoundary = callbackCalled == afterNext,
            Persisted = provider.GetRequiredService<CapturingOutbox>().Envelopes.Select(envelope => Deserialize(envelope).Value),
            pool.ContextCount,
            pool.ReturnCount
        }.MatchInlineSnapshot(
            """
            {
              "Error": "preparation failed",
              "CallbackMatchedBoundary": true,
              "Persisted": [
                "ordinary"
              ],
              "ContextCount": 1,
              "ReturnCount": 2
            }
            """);
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
            await PrepareAsync(bus, new TargetMessage("failed"), send: false,
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
    public async Task Complete_Should_CreateMiddlewareOnce_When_MultipleDispatchTargetsAreUsed()
    {
        // arrange
        var factories = new Dictionary<DispatchEndpoint, int>();
        var calls = 0;
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
            builder.ConfigureMessageBus(bus => bus.UseDispatch(new DispatchMiddlewareConfiguration(
                (factory, next) =>
                {
                    factories[factory.Endpoint] = factories.GetValueOrDefault(factory.Endpoint) + 1;
                    return context =>
                    {
                        calls++;
                        return next(context);
                    };
                },
                "CountFactories"))));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TargetMessage("ordinary"), CancellationToken.None);
        var stored = await CaptureAsync(bus, new TargetMessage("explicit"), send: false);

        // assert
        new
        {
            FactoryCallsPerEndpoint = factories.Values.Distinct().Order().ToArray(),
            DispatchCalls = calls,
            Stored = Deserialize(stored).Value
        }.MatchInlineSnapshot(
            """
            {
              "FactoryCallsPerEndpoint": [
                1
              ],
              "DispatchCalls": 2,
              "Stored": "explicit"
            }
            """);
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
    public async Task PrepareAsync_Should_SkipOutbox_When_MiddlewareShortCircuits(bool send)
    {
        // arrange
        var pool = new TrackingContextPool();
        var persisted = false;
        await using var provider = await CreateProviderAsync(outbox: true, builder =>
        {
            builder.Services.AddSingleton<ObjectPool<DispatchContext>>(pool);
            builder.ConfigureMessageBus(b => b.UseDispatch(
                new DispatchMiddlewareConfiguration(static (_, _) => static _ => default, "ShortCircuit")));
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await PrepareAsync(bus, new TargetMessage("incomplete"), send, (_, _) => persisted = true);

        // assert
        Assert.False(persisted);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(0, provider.GetRequiredService<TransportCounter>().Calls);
        Assert.Equal(1, pool.ReturnCount);
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
        var bus = (DefaultMessageBus)scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var outbox = new CallbackOutbox((_, _) => callbackCalled = true);
        if (!afterNext)
        {
            cancellation.Cancel();
        }

        // act
        var exception = await Record.ExceptionAsync(async () => await (send
            ? bus.SendAsync(new TargetMessage("cancelled"), SendOptions.Default, outbox.Configure, cancellation.Token)
            : bus.PublishAsync(new TargetMessage("cancelled"), PublishOptions.Default, outbox.Configure,
                cancellation.Token)));

        // assert
        Assert.Equal(afterNext ? null : typeof(OperationCanceledException), exception?.GetType());
        Assert.Equal(afterNext, entered);
        Assert.Equal(afterNext, callbackCalled);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(afterNext ? 1 : 0, pool.ReturnCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PrepareAsync_Should_UseExplicitOutbox_When_TimeIsSpecified(bool send, bool scheduledByMiddleware)
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
                new SendOptions { ScheduledTime = optionsScheduledTime }, target.Configure, CancellationToken.None)
            : dispatcher.PublishAsync(new TargetMessage("scheduled"),
                new PublishOptions { ScheduledTime = optionsScheduledTime }, target.Configure, CancellationToken.None));

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
    public async Task PrepareAsync_Should_PreserveScheduledTime_When_MiddlewareChangesTimeAfterNext(bool send)
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
                new SendOptions { ScheduledTime = time }, outbox.Configure, CancellationToken.None)
            : dispatcher.PublishAsync(new TargetMessage("scheduled"),
                new PublishOptions { ScheduledTime = time }, outbox.Configure, CancellationToken.None));

        // assert
        new
        {
            Message = Deserialize(captured!).Value,
            captured!.ScheduledTime,
            captured.SentAt,
            OutboxMessages = provider.GetRequiredService<CapturingOutbox>().Envelopes.Count,
            TransportCalls = provider.GetRequiredService<TransportCounter>().Calls
        }.MatchInlineSnapshot(
            """
            {
              "Message": "scheduled",
              "ScheduledTime": "2030-01-01T00:00:00+00:00",
              "SentAt": null,
              "OutboxMessages": 0,
              "TransportCalls": 0
            }
            """);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrepareAsync_Should_RejectBeforeStaging_When_NoSchedulerMatchesTransport(bool send)
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
                new SendOptions { ScheduledTime = time }, outbox.Configure, CancellationToken.None)
            : dispatcher.PublishAsync(new TargetMessage("scheduled"),
                new PublishOptions { ScheduledTime = time }, outbox.Configure, CancellationToken.None)));

        // assert
        Assert.False(staged);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(0, provider.GetRequiredService<TransportCounter>().Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScheduleAsync_Should_ReturnPersistedTime_When_MiddlewareChangesTimeAfterNext(bool send)
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
        var envelope = Assert.Single(store.Envelopes);
        new
        {
            result,
            envelope.ScheduledTime,
            Message = Deserialize(envelope).Value
        }.MatchInlineSnapshot(
            """
            {
              "result": {
                "Token": "test:scheduled",
                "ScheduledTime": "2030-01-01T00:00:00+00:00",
                "IsCancellable": true
              },
              "ScheduledTime": "2030-01-01T00:00:00+00:00",
              "Message": "scheduled"
            }
            """);
        Assert.Empty(provider.GetRequiredService<CapturingOutbox>().Envelopes);
        Assert.Equal(0, provider.GetRequiredService<TransportCounter>().Calls);
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
        plainProvider.GetRequiredService<TransportCounter>().CaptureEnvelopes = false;

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
            ((DefaultMessageBus)bus).PublishAsync(message, PublishOptions.Default, s_noopOutbox.Configure, CancellationToken.None)
                .GetAwaiter().GetResult();
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
            ? ((DefaultMessageBus)bus).SendAsync(message, SendOptions.Default, new CallbackOutbox(onPrepared).Configure, CancellationToken.None)
            : ((DefaultMessageBus)bus).PublishAsync(message, PublishOptions.Default, new CallbackOutbox(onPrepared).Configure, CancellationToken.None);

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
            if (counter.CaptureEnvelopes)
            {
                counter.Envelopes.Add(MessageEnvelope.CopyWithOwnedBody(context.Envelope!));
            }

            return default;
        }
    }

    private static readonly CallbackOutbox s_noopOutbox = new(static (_, _) => { });

    private sealed class CallbackOutbox : IMessageOutbox
    {
        private readonly Action<MessageEnvelope, CancellationToken> _callback;

        public CallbackOutbox(Action<MessageEnvelope, CancellationToken> callback)
        {
            _callback = callback;
            Configure = context => context.Features.Set<IMessageOutbox>(this);
        }

        public Action<IDispatchContext> Configure { get; }

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

        public bool CaptureEnvelopes { get; set; } = true;

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

    private sealed class TrackingContextPool : ObjectPool<DispatchContext>
    {
        private readonly DispatchContextPool _inner = new();
        private readonly HashSet<DispatchContext> _contexts = [];

        public int ContextCount => _contexts.Count;

        public int ReturnCount { get; private set; }

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
        }
    }
}
