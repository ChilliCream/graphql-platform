using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Mocha.Features;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

public sealed class SagaSchedulingCancellationTests
{
    private static readonly IMessagingRuntime s_runtime = CreateRuntime();

    private readonly TestMessageOutbox _outbox;
    private readonly TestMessageBus _bus;
    private readonly TestSagaStore _store;
    private readonly TestSagaCleanup _cleanup;
    private readonly FakeTimeProvider _timeProvider;
    private readonly IServiceProvider _services;

    public SagaSchedulingCancellationTests()
    {
        _store = new TestSagaStore();
        _cleanup = new TestSagaCleanup();
        _outbox = new TestMessageOutbox();
        _bus = new TestMessageBus(_outbox);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
        _services = new ServiceCollection()
            .AddSingleton<ISagaCleanup>(_cleanup)
            .AddSingleton<IMessageBus>(_bus)
            .AddSingleton<TimeProvider>(_timeProvider)
            .BuildServiceProvider();
    }

    [Fact]
    public async Task ScheduledPublish_Should_CaptureToken_When_TransitionSchedulesMessage()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(30);
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .ScheduledPublish(delay, _ => new ScheduledNotification())
                    .TransitionTo("Processing");

                x.During("Processing").OnEvent<EndEvent>().TransitionTo("Completed");

                x.Finally("Completed");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States.OfType<TestState>());
        Assert.NotNull(state.ScheduleTokens);
        Assert.Single(state.ScheduleTokens);
        Assert.StartsWith("test:", state.ScheduleTokens[0]);
    }

    [Fact]
    public async Task FinalState_Should_CancelAllTokens_When_SagaCompletes()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(30);
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .ScheduledPublish(delay, _ => new ScheduledNotification())
                    .TransitionTo("Processing");

                x.During("Processing").OnEvent<EndEvent>().TransitionTo("Completed");

                x.Finally("Completed");
            });

        Initialize(saga);

        // Act - transition to Processing (schedules a message)
        var context1 = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context1);

        var state = Assert.Single(_store.States.OfType<TestState>());
        var token = Assert.Single(state.ScheduleTokens!);

        // Act - transition to Completed (final state, should cancel)
        var context2 = CreateContext(saga, new EndEvent(), state.Id);
        await saga.HandleEvent(context2);

        // Assert
        Assert.Contains(token, _bus.CancelledTokens);
    }

    [Fact]
    public async Task FinalState_Should_CancelMultipleTokens_When_MultipleScheduledMessages()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(30);
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .ScheduledPublish(delay, _ => new ScheduledNotification())
                    .ScheduledSend(delay, _ => new ScheduledCommand())
                    .TransitionTo("Processing");

                x.During("Processing")
                    .OnEvent<MiddleEvent>()
                    .ScheduledPublish(delay, _ => new ScheduledNotification())
                    .TransitionTo("AwaitingCompletion");

                x.During("AwaitingCompletion").OnEvent<EndEvent>().TransitionTo("Completed");

                x.Finally("Completed");
            });

        Initialize(saga);

        // Act - transition to Processing (schedules 2 messages)
        var context1 = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context1);

        var state = Assert.Single(_store.States.OfType<TestState>());
        Assert.Equal(2, state.ScheduleTokens!.Count);

        // Act - transition to AwaitingCompletion (schedules 1 more)
        var context2 = CreateContext(saga, new MiddleEvent(), state.Id);
        await saga.HandleEvent(context2);

        state = Assert.Single(_store.States.OfType<TestState>());
        Assert.Equal(3, state.ScheduleTokens!.Count);
        var allTokens = state.ScheduleTokens.ToList();

        // Act - transition to Completed (final state, should cancel all 3)
        var context3 = CreateContext(saga, new EndEvent(), state.Id);
        await saga.HandleEvent(context3);

        // Assert
        Assert.Equal(3, _bus.CancelledTokens.Count);
        foreach (var token in allTokens)
        {
            Assert.Contains(token, _bus.CancelledTokens);
        }
    }

    [Fact]
    public async Task NonScheduledPublish_Should_NotCaptureToken_When_NoScheduledTime()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .Publish(_ => new ScheduledNotification())
                    .TransitionTo("Processing");

                x.During("Processing").OnEvent<EndEvent>().TransitionTo("Completed");

                x.Finally("Completed");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States.OfType<TestState>());
        Assert.Null(state.ScheduleTokens);
    }

    [Fact]
    public async Task NonFinalState_Should_NotCancelTokens_When_TransitionIsNotFinal()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(30);
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .ScheduledPublish(delay, _ => new ScheduledNotification())
                    .TransitionTo("Processing");

                x.During("Processing").OnEvent<MiddleEvent>().TransitionTo("AwaitingCompletion");

                x.During("AwaitingCompletion").OnEvent<EndEvent>().TransitionTo("Completed");

                x.Finally("Completed");
            });

        Initialize(saga);

        // Act - transition to Processing (schedules a message)
        var context1 = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context1);

        var state = Assert.Single(_store.States.OfType<TestState>());
        Assert.NotNull(state.ScheduleTokens);

        // Act - transition to AwaitingCompletion (non-final, should NOT cancel)
        var context2 = CreateContext(saga, new MiddleEvent(), state.Id);
        await saga.HandleEvent(context2);

        // Assert
        Assert.Empty(_bus.CancelledTokens);
        state = Assert.Single(_store.States.OfType<TestState>());
        Assert.NotNull(state.ScheduleTokens);
        Assert.Single(state.ScheduleTokens);
    }

    private TestConsumeContext CreateContext(Saga saga, object message, Guid? sagaId = null)
    {
        var context = new TestConsumeContext
        {
            CancellationToken = CancellationToken.None,
            CorrelationId = Guid.NewGuid().ToString(),
            MessageId = Guid.NewGuid().ToString(),
            Services = _services,
            Runtime = s_runtime
        };

        context.Features.Configure<MessageParsingFeature>(f => f.Message = message);
        context.Features.Configure<SagaFeature>(f => f.Store = _store);

        if (sagaId.HasValue)
        {
            context.MutableHeaders.Set(SagaContextData.SagaId, sagaId.Value.ToString("D"));
        }

        return context;
    }

    private static void Initialize(Saga saga)
    {
        saga.Initialize(TestMessagingSetupContext.Instance);
    }

    private sealed class TestState : SagaStateBase;

    private sealed class StartEvent;

    private sealed class MiddleEvent;

    private sealed class EndEvent;

    private sealed class ScheduledNotification;

    private sealed class ScheduledCommand;

    private static IMessagingRuntime CreateRuntime()
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddInMemory();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMessagingRuntime>();
    }
}
