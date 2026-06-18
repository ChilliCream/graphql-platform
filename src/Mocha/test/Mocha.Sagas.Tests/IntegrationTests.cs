using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

public class IntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    private static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    [Fact]
    public async Task Saga_Should_ExecuteThroughSteps()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<TestMessageHandler>();
            b.AddSaga<StepThroughSaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // act - publish InitEvent to start the saga
        await bus.PublishAsync(new InitEvent(), CancellationToken.None);

        // wait for the saga to publish TestMessage so we can get the saga ID
        Assert.True(await recorder.WaitAsync(s_timeout), "Saga did not publish TestMessage within timeout");

        var testMessage = Assert.Single(recorder.Messages);
        var sagaId = Assert.IsType<TestMessage>(testMessage).Id;

        // send first TriggerEvent to transition Started -> Triggered
        await bus.PublishAsync(new TriggerEvent(sagaId), CancellationToken.None);

        // wait until the first transition is persisted (state == "Triggered") before sending the
        // second event, so the two events are applied to the saga in order
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var sagaName = runtime.Naming.GetSagaName(typeof(StepThroughSaga));
        var transitionDeadline = DateTime.UtcNow + s_timeout;
        while (storage.Load<StepThroughState>(sagaName, sagaId)?.State != "Triggered"
            && DateTime.UtcNow < transitionDeadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        // fail fast if the first transition never happened, rather than sending the second event blindly
        Assert.Equal("Triggered", storage.Load<StepThroughState>(sagaName, sagaId)?.State);

        // send second TriggerEvent to transition Triggered -> Completed (final)
        await bus.PublishAsync(new TriggerEvent(sagaId), CancellationToken.None);

        // wait for the saga to reach its final state and be deleted from the store
        var deadline = DateTime.UtcNow + s_timeout;
        while (storage.Count != 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        // assert - saga should be deleted from store after reaching final state
        Assert.Equal(0, storage.Count);
    }

    [Fact]
    public async Task Saga_Should_Timeout()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<TriggerEventRecorder>();
            b.AddSaga<TimeoutSaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // act - publish TriggerEvent to start the saga (no correlation -> creates new)
        await bus.PublishAsync(new StartTimeoutEvent(), CancellationToken.None);

        // wait for the saga to publish a TriggerEvent so we can observe it started
        Assert.True(await recorder.WaitAsync(s_timeout), "Saga did not publish event within timeout");

        var recorded = Assert.Single(recorder.Messages);
        var sagaId = Assert.IsType<TriggerEvent>(recorded).CorrelationId!.Value;

        // wait until the saga instance is persisted before sending the timeout, so the
        // SagaTimedOutEvent is applied to the stored instance
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var sagaName = runtime.Naming.GetSagaName(typeof(TimeoutSaga));
        var persistDeadline = DateTime.UtcNow + s_timeout;
        while (storage.Load<TimeoutState>(sagaName, sagaId) is null && DateTime.UtcNow < persistDeadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        // fail fast if the saga was never persisted, so the timeout below targets the stored instance
        Assert.NotNull(storage.Load<TimeoutState>(sagaName, sagaId));

        // simulate timeout by sending SagaTimedOutEvent with the saga ID
        await bus.SendAsync(new SagaTimedOutEvent(sagaId), CancellationToken.None);

        // wait for the saga to reach its final state and be deleted from the store
        var deadline = DateTime.UtcNow + s_timeout;
        while (storage.Load<TimeoutState>(sagaName, sagaId) is not null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        // assert - saga should be deleted from store after reaching final state
        Assert.Equal(0, storage.Count);
    }

    [Fact]
    public async Task Saga_Should_TimeoutWithCustomResponse()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<TriggerEventRecorder>();
            b.AddSaga<TimeoutWithResponseSaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // act - publish StartTimeoutEvent to start the saga
        await bus.PublishAsync(new StartTimeoutEvent(), CancellationToken.None);

        // wait for the saga to publish a TriggerEvent so we can observe it started
        Assert.True(await recorder.WaitAsync(s_timeout), "Saga did not publish event within timeout");

        var recorded = Assert.Single(recorder.Messages);
        var sagaId = Assert.IsType<TriggerEvent>(recorded).CorrelationId!.Value;

        // wait until the saga instance is persisted before sending the timeout, so the
        // SagaTimedOutEvent is applied to the stored instance
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var sagaName = runtime.Naming.GetSagaName(typeof(TimeoutWithResponseSaga));
        var persistDeadline = DateTime.UtcNow + s_timeout;
        while (storage.Load<TimeoutState>(sagaName, sagaId) is null && DateTime.UtcNow < persistDeadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        // fail fast if the saga was never persisted, so the timeout below targets the stored instance
        Assert.NotNull(storage.Load<TimeoutState>(sagaName, sagaId));

        // simulate timeout by sending SagaTimedOutEvent with the saga ID
        await bus.SendAsync(new SagaTimedOutEvent(sagaId), CancellationToken.None);

        // wait for the saga to reach its final state and be deleted from the store
        var deadline = DateTime.UtcNow + s_timeout;
        while (storage.Load<TimeoutState>(sagaName, sagaId) is not null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        // assert - saga should be deleted from store after reaching final state
        Assert.Equal(0, storage.Count);
    }

    [Fact]
    public async Task Saga_Should_SupportRequestResponse()
    {
        // arrange
        var recorder = new MessageRecorder();
        var replyGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddSingleton(replyGate);
            b.AddRequestHandler<GatedTriggerRequestHandler>();
            b.AddSaga<RequestResponseSaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // act - publish StartRequestEvent to start the saga, which sends TriggerRequest via .Send
        await bus.PublishAsync(new StartRequestEvent(), CancellationToken.None);

        // the handler parks on the reply gate, holding the saga in AwaitingResponse so the test can
        // observe its persisted state before releasing the reply to finalize the saga
        Assert.True(await recorder.WaitAsync(s_timeout), "request handler never executed");

        // wait until the held saga's state is persisted, and assert it actually appeared
        var appearDeadline = DateTime.UtcNow + s_timeout;
        while (storage.Count == 0 && DateTime.UtcNow < appearDeadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        Assert.True(storage.Count > 0, "saga state was not persisted while the reply was held");

        // release the reply so it routes back via OnAnyReply and finalizes the saga
        replyGate.SetResult();

        // wait for the reply to finalize the saga and delete its state
        var deadline = DateTime.UtcNow + s_timeout;
        while (storage.Count != 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        // assert - saga should be deleted from store after reaching final state
        Assert.Equal(0, storage.Count);
    }

    [Fact]
    public async Task Saga_Should_ReceiveReply_When_SendUsedWithOnReply()
    {
        // A saga that uses .Send to dispatch a request and .OnReply (or .OnAnyReply) to handle the
        // response routes the reply back to its own durable endpoint and correlates it by the saga
        // header, even though the reply type does not match any subscribed route. This test isolates
        // the reply leg: the handler runs, the reply is routed to the saga, and the saga finalizes.

        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<RecordingTriggerRequestHandler>();
            b.AddSaga<RequestResponseSaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // act - publish StartRequestEvent to start the saga, which sends TriggerRequest via .Send
        await bus.PublishAsync(new StartRequestEvent(), CancellationToken.None);

        // assert - the handler runs, proving the request was delivered (this isolates the reply leg)
        Assert.True(await recorder.WaitAsync(s_timeout), "request handler never executed");

        // give the reply time to route back to the saga and finalize it
        var deadline = DateTime.UtcNow + s_timeout;
        while (storage.Count != 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        // assert - the reply routed back to the saga, transitioned it to its final state, and the
        // saga state was deleted from the store (storage.Count reaches 0).
        Assert.True(
            storage.Count == 0,
            "the request handler executed but the reply was not routed back to the saga: the saga "
                + "did not reach its final state and its state was not deleted from the store.");
    }

    [Fact]
    public async Task Saga_Should_ReceiveReply_When_SendUsedWithTypedOnReply()
    {
        // A saga that uses .Send and a typed .OnReply<TriggerResponse>() (not the object based
        // OnAnyReply) must finalize. This proves a typed reply resolves its concrete type on the
        // shared reply endpoint and selects the typed reply route.

        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<RecordingTriggerRequestHandler>();
            b.AddSaga<TypedReplySaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // act - publish StartRequestEvent to start the saga, which sends TriggerRequest via .Send
        await bus.PublishAsync(new StartRequestEvent(), CancellationToken.None);

        // assert - the handler runs, proving the request was delivered
        Assert.True(await recorder.WaitAsync(s_timeout), "request handler never executed");

        // give the typed reply time to route back to the saga and finalize it
        var deadline = DateTime.UtcNow + s_timeout;
        while (storage.Count != 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        // assert - the typed reply routed back to the saga and finalized it
        Assert.True(
            storage.Count == 0,
            "the request handler executed but the typed reply was not routed back to the saga: the "
                + "saga did not reach its final state and its state was not deleted from the store.");
    }

    [Fact]
    public async Task Sagas_Should_EachOwnTheirReply_When_TwoSagasUseOnAnyReply()
    {
        // Two sagas both declare OnAnyReply, so a saga-id reply selects both saga consumers. The
        // condition selects the consumer, never the instance. The saga that owns the reply's saga-id
        // transitions and finalizes; the other loads no instance for that id and no-ops, with no
        // phantom create. Both sagas reach their final state and the store ends empty.

        // arrange
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddRequestHandler<TriggerRequestHandler>();
            b.AddRequestHandler<SecondTriggerRequestHandler>();
            b.AddSaga<RequestResponseSaga>();
            b.AddSaga<SecondRequestResponseSaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // act - start both sagas; each sends its own request and awaits its own reply
        await bus.PublishAsync(new StartRequestEvent(), CancellationToken.None);
        await bus.PublishAsync(new StartSecondRequestEvent(), CancellationToken.None);

        var deadline = DateTime.UtcNow + s_timeout;
        while (storage.Count != 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        // assert - both sagas finalized and were removed, with no phantom instances left behind
        Assert.Equal(0, storage.Count);
    }

    [Fact]
    public async Task RpcReply_Should_NotSelectSaga_When_SameResponseTypeRequestedDirectly()
    {
        // RPC-contamination regression: a saga declares OnReply<TriggerResponse> and the same process
        // issues a direct bus.RequestAsync of the same response type. The RPC reply carries no saga-id
        // header, so the saga reply route's saga-id gate excludes it: the RPC round-trips and the saga
        // is never selected (which would otherwise fault via CreateState).

        // arrange
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddRequestHandler<TriggerRequestHandler>();
            b.AddSaga<TypedReplySaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // act - a direct RPC request of the saga's reply type, with no saga running
        var response = await bus.RequestAsync(new TriggerRequest(), CancellationToken.None);

        // assert - the RPC round-trips and no saga state was created from the RPC reply
        Assert.IsType<TriggerResponse>(response);
        Assert.Equal(0, storage.Count);
    }

    [Fact]
    public async Task Bus_Should_RoundTripReply_When_RequestAsyncUsedDirectly()
    {
        // baseline: a direct bus.RequestAsync sets a CorrelationId and registers a deferred response
        // promise, so the reply round-trips. This contrasts the RPC path with the saga .Send path.

        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<RecordingTriggerRequestHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var response = await bus.RequestAsync(new TriggerRequest(), CancellationToken.None);

        // assert
        Assert.NotNull(response);
        Assert.IsType<TriggerResponse>(response);
    }

    // =========================================================================
    // Saga Definitions
    // =========================================================================

    public sealed class StepThroughState : SagaStateBase;

    public sealed class TimeoutState : SagaStateBase;

    public sealed class RequestResponseState : SagaStateBase;

    /// <summary>
    /// Multi-step saga: InitEvent -> Started (publishes TestMessage) ->
    /// TriggerEvent -> Triggered -> TriggerEvent -> Completed (final)
    /// </summary>
    public sealed class StepThroughSaga : Saga<StepThroughState>
    {
        protected override void Configure(ISagaDescriptor<StepThroughState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<InitEvent>()
                .StateFactory(_ => new StepThroughState())
                .Publish((_, state) => new TestMessage(state.Id))
                .TransitionTo("Started");

            descriptor.During("Started").OnEvent<TriggerEvent>().TransitionTo("Triggered");

            descriptor.During("Triggered").OnEvent<TriggerEvent>().TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }

    /// <summary>
    /// Timeout saga: StartTimeoutEvent -> Active (publishes TriggerEvent with saga ID) ->
    /// SagaTimedOutEvent -> TimedOut (final)
    /// </summary>
    public sealed class TimeoutSaga : Saga<TimeoutState>
    {
        protected override void Configure(ISagaDescriptor<TimeoutState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartTimeoutEvent>()
                .StateFactory(_ => new TimeoutState())
                .Publish((_, state) => new TriggerEvent(state.Id))
                .TransitionTo("Active");

            descriptor.During("Active").OnTimeout().TransitionTo("TimedOut");

            descriptor.Finally("TimedOut");
        }
    }

    /// <summary>
    /// Timeout saga with Timeout() API: StartTimeoutEvent -> Active (publishes TriggerEvent) ->
    /// SagaTimedOutEvent -> TimedOut (final, with custom response possible)
    /// </summary>
    public sealed class TimeoutWithResponseSaga : Saga<TimeoutState>
    {
        protected override void Configure(ISagaDescriptor<TimeoutState> descriptor)
        {
            descriptor.Timeout(TimeSpan.FromMinutes(30));

            descriptor
                .Initially()
                .OnEvent<StartTimeoutEvent>()
                .StateFactory(_ => new TimeoutState())
                .Publish((_, state) => new TriggerEvent(state.Id))
                .TransitionTo("Active");

            descriptor.DuringAny().OnTimeout().TransitionTo(StateNames.TimedOut);

            descriptor.During("Active").OnEvent<EndTimeoutEvent>().TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }

    /// <summary>
    /// Request-response saga: StartRequestEvent -> AwaitingResponse (sends TriggerRequest) ->
    /// any reply -> Completed (final)
    /// </summary>
    public sealed class RequestResponseSaga : Saga<RequestResponseState>
    {
        protected override void Configure(ISagaDescriptor<RequestResponseState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartRequestEvent>()
                .StateFactory(_ => new RequestResponseState())
                .Send((_, _) => new TriggerRequest())
                .TransitionTo("AwaitingResponse");

            descriptor.During("AwaitingResponse").OnAnyReply().TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }

    /// <summary>
    /// Second request-response saga: StartSecondRequestEvent -> AwaitingResponse (sends
    /// SecondTriggerRequest) -> any reply -> Completed (final). Used to prove OnAnyReply fan-out is
    /// safe across two sagas.
    /// </summary>
    public sealed class SecondRequestResponseSaga : Saga<RequestResponseState>
    {
        protected override void Configure(ISagaDescriptor<RequestResponseState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartSecondRequestEvent>()
                .StateFactory(_ => new RequestResponseState())
                .Send((_, _) => new SecondTriggerRequest())
                .TransitionTo("AwaitingResponse");

            descriptor.During("AwaitingResponse").OnAnyReply().TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }

    /// <summary>
    /// Typed-reply saga: StartRequestEvent -> AwaitingResponse (sends TriggerRequest) ->
    /// typed OnReply&lt;TriggerResponse&gt; -> Completed (final)
    /// </summary>
    public sealed class TypedReplySaga : Saga<RequestResponseState>
    {
        protected override void Configure(ISagaDescriptor<RequestResponseState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartRequestEvent>()
                .StateFactory(_ => new RequestResponseState())
                .Send((_, _) => new TriggerRequest())
                .TransitionTo("AwaitingResponse");

            descriptor.During("AwaitingResponse").OnReply<TriggerResponse>().TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }

    // =========================================================================
    // Events & Messages
    // =========================================================================

    public sealed class InitEvent;

    public sealed class StartTimeoutEvent;

    public sealed class EndTimeoutEvent;

    public sealed class StartRequestEvent;

    public sealed class StartSecondRequestEvent;

    public sealed record TriggerEvent(Guid? CorrelationId) : ICorrelatable;

    public sealed record TestMessage(Guid Id);

    public sealed record TriggerRequest : IEventRequest<TriggerResponse>;

    public sealed record TriggerResponse;

    public sealed record SecondTriggerRequest : IEventRequest<SecondTriggerResponse>;

    public sealed record SecondTriggerResponse;

    // =========================================================================
    // Handlers
    // =========================================================================

    public sealed class TestMessageHandler(MessageRecorder recorder) : IEventHandler<TestMessage>
    {
        public ValueTask HandleAsync(TestMessage message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class TriggerEventRecorder(MessageRecorder recorder) : IEventHandler<TriggerEvent>
    {
        public ValueTask HandleAsync(TriggerEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    private sealed class TriggerRequestHandler : IEventRequestHandler<TriggerRequest, TriggerResponse>
    {
        public ValueTask<TriggerResponse> HandleAsync(TriggerRequest request, CancellationToken cancellationToken)
        {
            return new(new TriggerResponse());
        }
    }

    private sealed class SecondTriggerRequestHandler
        : IEventRequestHandler<SecondTriggerRequest, SecondTriggerResponse>
    {
        public ValueTask<SecondTriggerResponse> HandleAsync(
            SecondTriggerRequest request,
            CancellationToken cancellationToken)
        {
            return new(new SecondTriggerResponse());
        }
    }

    private sealed class RecordingTriggerRequestHandler(MessageRecorder recorder)
        : IEventRequestHandler<TriggerRequest, TriggerResponse>
    {
        public ValueTask<TriggerResponse> HandleAsync(TriggerRequest request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return new(new TriggerResponse());
        }
    }

    // Signals that the request was received, then parks until the test releases the reply gate.
    // Holding the reply keeps the saga in its AwaitingResponse state so the test can deterministically
    // observe the persisted state before triggering finalization.
    private sealed class GatedTriggerRequestHandler(MessageRecorder recorder, TaskCompletionSource replyGate)
        : IEventRequestHandler<TriggerRequest, TriggerResponse>
    {
        public async ValueTask<TriggerResponse> HandleAsync(TriggerRequest request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            await replyGate.Task.WaitAsync(cancellationToken);
            return new TriggerResponse();
        }
    }

    // =========================================================================
    // Test Infrastructure
    // =========================================================================

    public sealed class MessageRecorder
    {
        private readonly SemaphoreSlim _semaphore = new(0);

        public ConcurrentBag<object> Messages { get; } = [];

        public void Record(object message)
        {
            Messages.Add(message);
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
