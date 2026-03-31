using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Mocha.Features;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

public sealed class SagaSchedulingTests
{
    private static readonly IMessagingRuntime s_runtime = CreateRuntime();

    private readonly TestMessageOutbox _outbox;
    private readonly TestSagaStore _store;
    private readonly TestSagaCleanup _cleanup;
    private readonly FakeTimeProvider _timeProvider;
    private readonly IServiceProvider _services;

    private static IMessagingRuntime CreateRuntime()
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddInMemory();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMessagingRuntime>();
    }

    public SagaSchedulingTests()
    {
        _store = new TestSagaStore();
        _cleanup = new TestSagaCleanup();
        _outbox = new TestMessageOutbox();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
        _services = new ServiceCollection()
            .AddSingleton<ISagaCleanup>(_cleanup)
            .AddSingleton<IMessageBus>(new TestMessageBus(_outbox))
            .AddSingleton<TimeProvider>(_timeProvider)
            .BuildServiceProvider();
    }

    [Fact]
    public async Task ScheduledPublish_Should_ProducePublishOptionsWithScheduledTime_When_TransitionFires()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(5);
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .ScheduledPublish(delay, _ => new ScheduledNotification())
                    .TransitionTo("Started");

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context);

        // Assert
        var operation = Assert.Single(_outbox.Messages);
        Assert.IsType<ScheduledNotification>(operation.Message);
        var options = Assert.IsType<PublishOptions>(operation.Options);
        Assert.Equal(_timeProvider.GetUtcNow().Add(delay), options.ScheduledTime);
    }

    [Fact]
    public async Task ScheduledSend_Should_ProduceSendOptionsWithScheduledTime_When_TransitionFires()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(10);
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .ScheduledSend(delay, _ => new ScheduledCommand())
                    .TransitionTo("Started");

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context);

        // Assert
        var operation = Assert.Single(_outbox.Messages);
        Assert.IsType<ScheduledCommand>(operation.Message);
        var options = Assert.IsType<SendOptions>(operation.Options);
        Assert.Equal(_timeProvider.GetUtcNow().Add(delay), options.ScheduledTime);
    }

    [Fact]
    public async Task ScheduledPublish_Lifecycle_Should_ProducePublishOptionsWithScheduledTime_When_OnEntryFires()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(5);
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .TransitionTo("Started");

                x.During("Started")
                    .OnEntry()
                    .ScheduledPublish(delay, _ => new ScheduledNotification());

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context);

        // Assert
        var operation = Assert.Single(_outbox.Messages);
        Assert.IsType<ScheduledNotification>(operation.Message);
        var options = Assert.IsType<PublishOptions>(operation.Options);
        Assert.Equal(_timeProvider.GetUtcNow().Add(delay), options.ScheduledTime);
    }

    [Fact]
    public async Task ScheduledSend_Lifecycle_Should_ProduceSendOptionsWithScheduledTime_When_OnEntryFires()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(10);
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .TransitionTo("Started");

                x.During("Started")
                    .OnEntry()
                    .ScheduledSend(delay, _ => new ScheduledCommand());

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context);

        // Assert
        var operation = Assert.Single(_outbox.Messages);
        Assert.IsType<ScheduledCommand>(operation.Message);
        var options = Assert.IsType<SendOptions>(operation.Options);
        Assert.Equal(_timeProvider.GetUtcNow().Add(delay), options.ScheduledTime);
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

        context.Features.Configure<MessageParsingFeature>(f => f.Message = message);
        context.Features.Configure<SagaFeature>(f => f.Store = _store);

        return context;
    }

    private static void Initialize(Saga saga)
    {
        saga.Initialize(TestMessagingSetupContext.Instance);
    }

    private sealed class TestState : SagaStateBase;

    private sealed class StartEvent;

    private sealed class EndEvent;

    private sealed class ScheduledNotification;

    private sealed class ScheduledCommand;
}
