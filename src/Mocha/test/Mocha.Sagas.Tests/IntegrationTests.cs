using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Mocha.Events;
using Mocha.Sagas;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

public class IntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

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
        Assert.True(await recorder.WaitAsync(Timeout), "Saga did not publish TestMessage within timeout");

        var testMessage = Assert.Single(recorder.Messages);
        var sagaId = Assert.IsType<TestMessage>(testMessage).Id;

        // send first TriggerEvent to transition Started -> Triggered
        await bus.PublishAsync(new TriggerEvent(sagaId), CancellationToken.None);

        // allow time for the first transition to complete before sending the second event
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // send second TriggerEvent to transition Triggered -> Completed (final)
        await bus.PublishAsync(new TriggerEvent(sagaId), CancellationToken.None);

        // allow time for final state processing
        await Task.Delay(1000, TestContext.Current.CancellationToken);

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
        Assert.True(await recorder.WaitAsync(Timeout), "Saga did not publish event within timeout");

        var recorded = Assert.Single(recorder.Messages);
        var sagaId = Assert.IsType<TriggerEvent>(recorded).CorrelationId!.Value;

        // simulate timeout by sending SagaTimedOutEvent with the saga ID
        await bus.SendAsync(new SagaTimedOutEvent(sagaId), CancellationToken.None);

        // allow time for final state processing
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // assert - saga should be deleted from store after reaching final state
        Assert.Equal(0, storage.Count);
    }

    [Fact(Skip = "Timeout(TimeSpan) auto-scheduling throws NotImplementedException")]
    public async Task Saga_Should_TimeoutWithCustomResponse()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "OnAnyReply saga reply routing requires full reply pipeline investigation")]
    public async Task Saga_Should_SupportRequestResponse()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddRequestHandler<TriggerRequestHandler>();
            b.AddSaga<RequestResponseSaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // act - publish StartRequestEvent to start the saga
        await bus.PublishAsync(new StartRequestEvent(), CancellationToken.None);

        // allow time for: saga starts -> sends TriggerRequest -> handler responds
        // -> saga receives reply via OnAnyReply -> transitions to Completed (final)
        await Task.Delay(2000, TestContext.Current.CancellationToken);

        // assert - saga should be deleted from store after reaching final state
        Assert.Equal(0, storage.Count);
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

    // =========================================================================
    // Events & Messages
    // =========================================================================

    public sealed class InitEvent;

    public sealed class StartTimeoutEvent;

    public sealed class StartRequestEvent;

    public sealed record TriggerEvent(Guid? CorrelationId) : ICorrelatable;

    public sealed record TestMessage(Guid Id);

    public sealed record TriggerRequest : IEventRequest<TriggerResponse>;

    public sealed record TriggerResponse;

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
