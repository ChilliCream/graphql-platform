using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Mocha.Features;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

public sealed class SagaTimeoutTests
{
    private static readonly IMessagingRuntime s_runtime = CreateRuntime();

    private readonly TestMessageOutbox _outbox;
    private readonly TestSagaStore _store;
    private readonly TestSagaCleanup _cleanup;
    private readonly FakeTimeProvider _timeProvider;
    private readonly TestMessageBus _bus;
    private readonly IServiceProvider _services;

    private static IMessagingRuntime CreateRuntime()
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddInMemory();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMessagingRuntime>();
    }

    public SagaTimeoutTests()
    {
        _store = new TestSagaStore();
        _cleanup = new TestSagaCleanup();
        _outbox = new TestMessageOutbox();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
        _bus = new TestMessageBus(_outbox);
        _services = new ServiceCollection()
            .AddSingleton<ISagaCleanup>(_cleanup)
            .AddSingleton<IMessageBus>(_bus)
            .AddSingleton<TimeProvider>(_timeProvider)
            .BuildServiceProvider();
    }

    [Fact]
    public async Task Timeout_Should_ScheduleTimedOutEvent_When_SagaCreated()
    {
        // Arrange
        var timeout = TimeSpan.FromMinutes(5);
        var saga =
            Saga.Create<TimeoutState>(x =>
            {
                x.Timeout(timeout);

                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TimeoutState()).TransitionTo("Active");

                x.DuringAny().OnTimeout().TransitionTo(StateNames.TimedOut);

                x.During("Active").OnEvent<EndEvent>().TransitionTo("Completed");

                x.Finally("Completed");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context);

        // Assert - ScheduleSendAsync should have been called with a SagaTimedOutEvent
        var scheduledOp = _outbox.Messages.FirstOrDefault(m => m.Message is SagaTimedOutEvent);
        Assert.NotNull(scheduledOp);

        var timedOutEvent = Assert.IsType<SagaTimedOutEvent>(scheduledOp.Message);
        var state = Assert.Single(_store.States);
        Assert.Equal(state.Id, timedOutEvent.SagaId);

        // Verify the scheduled time matches now + timeout
        Assert.Equal(TestMessageOutbox.OperationKind.Send, scheduledOp.Kind);
    }

    [Fact]
    public async Task Timeout_Should_StoreToken_When_SagaCreated()
    {
        // Arrange
        var saga =
            Saga.Create<TimeoutState>(x =>
            {
                x.Timeout(TimeSpan.FromMinutes(5));

                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TimeoutState()).TransitionTo("Active");

                x.DuringAny().OnTimeout().TransitionTo(StateNames.TimedOut);

                x.During("Active").OnEvent<EndEvent>().TransitionTo("Completed");

                x.Finally("Completed");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States);
        Assert.NotNull(state.TimeoutToken);
        Assert.StartsWith("test:", state.TimeoutToken);
    }

    [Fact]
    public async Task Timeout_Should_CancelToken_When_SagaReachesFinalState()
    {
        // Arrange
        var saga =
            Saga.Create<TimeoutState>(x =>
            {
                x.Timeout(TimeSpan.FromMinutes(5));

                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TimeoutState()).TransitionTo("Active");

                x.DuringAny().OnTimeout().TransitionTo(StateNames.TimedOut);

                x.During("Active").OnEvent<EndEvent>().TransitionTo("Completed");

                x.Finally("Completed");
            });

        Initialize(saga);

        // Act - create saga
        var createContext = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(createContext);

        var state = Assert.Single(_store.States);
        var token = state.TimeoutToken;
        Assert.NotNull(token);

        // Act - send event that transitions to final state
        var endContext = CreateContextWithSagaId(saga, new EndEvent(), state.Id);
        await saga.HandleEvent(endContext);

        // Assert - token should have been cancelled
        Assert.Contains(token, _bus.CancelledTokens);
        // Saga should be deleted from store
        Assert.Empty(_store.States);
    }

    [Fact]
    public async Task Timeout_Should_TransitionToTimedOut_When_TimeoutEventReceived()
    {
        // Arrange
        var saga =
            Saga.Create<TimeoutState>(x =>
            {
                x.Timeout(TimeSpan.FromMinutes(5));

                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TimeoutState()).TransitionTo("Active");

                x.DuringAny().OnTimeout().TransitionTo(StateNames.TimedOut);

                x.During("Active").OnEvent<EndEvent>().TransitionTo("Completed");

                x.Finally("Completed");
            });

        Initialize(saga);

        // Act - create saga
        var createContext = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(createContext);

        var state = Assert.Single(_store.States);
        var sagaId = state.Id;

        // Act - simulate timeout by sending SagaTimedOutEvent
        var timeoutContext = CreateContextWithSagaId(saga, new SagaTimedOutEvent(sagaId), sagaId);
        await saga.HandleEvent(timeoutContext);

        // Assert - saga should be deleted (final state reached)
        Assert.Empty(_store.States);
    }

    [Fact]
    public async Task NoTimeout_Should_NotSchedule_When_TimeoutNotConfigured()
    {
        // Arrange - no Timeout() call
        var saga =
            Saga.Create<TimeoutState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TimeoutState()).TransitionTo("Active");

                x.During("Active").OnEvent<EndEvent>().TransitionTo("Completed");

                x.Finally("Completed");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context);

        // Assert - no SagaTimedOutEvent should be scheduled
        var scheduledOp = _outbox.Messages.FirstOrDefault(m => m.Message is SagaTimedOutEvent);
        Assert.Null(scheduledOp);

        // State should not have a timeout token
        var state = Assert.Single(_store.States);
        Assert.Null(state.TimeoutToken);
    }

    private TestConsumeContext CreateContext(Saga saga, object message)
    {
        var context = new TestConsumeContext
        {
            CancellationToken = CancellationToken.None,
            CorrelationId = Guid.NewGuid().ToString(),
            MessageId = Guid.NewGuid().ToString(),
            Services = _services,
            Runtime = s_runtime
        };

        context.Features.GetOrSet<MessageParsingFeature>().Message = message;
        context.Features.GetOrSet<SagaFeature>().Store = _store;

        return context;
    }

    private TestConsumeContext CreateContextWithSagaId(Saga saga, object message, Guid sagaId)
    {
        var context = CreateContext(saga, message);
        context.MutableHeaders.Set(SagaContextData.SagaId, sagaId.ToString("D"));
        return context;
    }

    private static void Initialize(Saga saga)
    {
        saga.Initialize(TestMessagingSetupContext.Instance);
    }

    private sealed class TimeoutState : SagaStateBase;

    private sealed class StartEvent;

    private sealed class EndEvent;
}
