using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Mocha.Features;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

public sealed class SagaStateMachineTests
{
    private static readonly IMessagingRuntime _runtime = CreateRuntime();

    private readonly TestMessageOutbox _outbox;
    private readonly TestSagaStore _store;
    private readonly TestSagaCleanup _cleanup;
    private readonly IServiceProvider _services;

    private static IMessagingRuntime CreateRuntime()
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddInMemory();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMessagingRuntime>();
    }

    public SagaStateMachineTests()
    {
        _store = new TestSagaStore();
        _cleanup = new TestSagaCleanup();
        _outbox = new TestMessageOutbox();
        _services = new ServiceCollection()
            .AddSingleton<ISagaCleanup>(_cleanup)
            .AddSingleton<IMessageBus>(new TestMessageBus(_outbox))
            .BuildServiceProvider();
    }

    [Fact]
    public async Task Sage_Should_Initialize_State_On_InitialEvent()
    {
        // Arrange
        var initState = new TestState();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => initState).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new Start());
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States);
        Assert.Equal("Started", state.State);
        Assert.Same(initState, state);
    }

    [Fact]
    public async Task Saga_Should_Not_InitializeState_When_InvalidEvent()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new End());

        var ex = await Assert.ThrowsAsync<SagaExecutionException>(() => saga.HandleEvent(context));

        // Assert
        Assert.Equal("No transition defined for event 'End' in state '__Initial'.", ex.Message);
    }

    [Fact]
    public async Task Sage_Should_CallLifeCycleOnEntryPublish_When_Initial()
    {
        // Arrange
        var initState = new TestState();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEntry().Publish(_ => new Publish());

                x.Initially().OnEvent<Start>().StateFactory(_ => initState).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new Start());
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.IsType<Publish>(message.Message);
    }

    [Fact]
    public async Task Sage_Should_CallLifeCycleOnEntrySend_When_Initial()
    {
        // Arrange
        var initState = new TestState();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEntry().Send(_ => new Send());

                x.Initially().OnEvent<Start>().StateFactory(_ => initState).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new Start());
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.IsType<Send>(message.Message);
    }

    [Fact]
    public async Task Saga_Should_StoreMetadata_When_Init()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new Start());
        context.ResponseAddress = new Uri("http://test/reply");
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States);
        Assert.Equal(context.CorrelationId, state.Metadata.GetValue("correlation-id"));
        Assert.Equal("http://test/reply", state.Metadata.GetValue("saga-reply-address"));
    }

    [Fact]
    public async Task Saga_Should_PublishEvents_OnInit()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new Start());
        context.ResponseAddress = new Uri("http://test/reply");
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States);
        Assert.Equal(context.CorrelationId, state.Metadata.GetValue("correlation-id"));
        Assert.Equal("http://test/reply", state.Metadata.GetValue("saga-reply-address"));
    }

    [Fact]
    public async Task Saga_Should_PublishEvent_OnInit()
    {
        // Arrange
        var publishEvent = new Publish();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<Start>()
                    .StateFactory(_ => new TestState())
                    .Publish((_, _) => publishEvent)
                    .TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new Start());
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.Same(publishEvent, message.Message);
    }

    [Fact]
    public async Task Saga_Should_SendEvent_OnInit()
    {
        // Arrange
        var sendEvent = new Send();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<Start>()
                    .StateFactory(_ => new TestState())
                    .Send((_, _) => sendEvent)
                    .TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new Start());
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.Same(sendEvent, message.Message);
    }

    [Fact]
    public async Task Saga_Should_CallAction_When_EventIsReceivedOnInit()
    {
        // Arrange
        var actionCalled = false;
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<Start>()
                    .StateFactory(_ => new TestState())
                    .TransitionTo("Started")
                    .Then((_, _) => actionCalled = true);

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new Start());
        await saga.HandleEvent(context);

        // Assert
        Assert.True(actionCalled);
    }

    [Fact]
    public async Task Saga_Should_TransitionToNextState_When_EventsAreHandled()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<Trigger>().TransitionTo("Triggered");

                x.During("Triggered").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new Trigger { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States);
        Assert.Equal("Triggered", state.State);
    }

    [Fact]
    public async Task Saga_Should_FailOnTransition_When_IllegalSagaState()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<Trigger>().TransitionTo("Triggered");

                x.During("Triggered").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = "NOPE", Id = Guid.NewGuid() };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new Trigger { CorrelationId = initState.Id });
        var ex = await Assert.ThrowsAsync<SagaExecutionException>(() => saga.HandleEvent(context));

        // Assert
        Assert.Equal("No state found for state 'NOPE'.", ex.Message);
    }

    [Fact]
    public async Task Saga_Should_ReturnFalse_When_InvalidCorrelationId()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<Trigger>().TransitionTo("Triggered");

                x.During("Triggered").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = "Started", Id = Guid.NewGuid() };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(
            saga,
            new Trigger { CorrelationId = Guid.Parse("B396C409-F0D5-4888-A6D2-A940482ADC4B") });
        var result = await saga.HandleEvent(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Saga_Should_Throw_When_EventHasNoTransitionDefined()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<Trigger>().TransitionTo("Triggered");

                x.During("Triggered").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new End { CorrelationId = initState.Id });

        var ex = await Assert.ThrowsAsync<SagaExecutionException>(() => saga.HandleEvent(context));

        // Assert
        Assert.Equal("No transition defined for event 'End' in state 'Started'.", ex.Message);
    }

    [Fact]
    public async Task Saga_Should_Fallback_When_AnyEvent()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnAnyReply().TransitionTo("Triggered");

                x.During("Triggered").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new End { CorrelationId = initState.Id });

        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States);
        Assert.Equal("Triggered", state.State);
    }

    [Fact]
    public async Task Saga_Should_CallAction_When_EventIsReceivedOnTransition()
    {
        // Arrange
        var actionCalled = false;
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<Trigger>().TransitionTo("Triggered").Then((_, _) => actionCalled = true);

                x.During("Triggered").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new Trigger { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        Assert.True(actionCalled);
    }

    [Fact]
    public async Task Saga_Should_PublishEvent_OnTransition()
    {
        // Arrange
        var publishEvent = new Publish();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<Trigger>().TransitionTo("Triggered").Publish((_, _) => publishEvent);

                x.During("Triggered").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new Trigger { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.Same(publishEvent, message.Message);
    }

    [Fact]
    public async Task Saga_Should_SendEvent_OnTransition()
    {
        // Arrange
        var sendEvent = new Send();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<Trigger>().TransitionTo("Triggered").Send((_, _) => sendEvent);

                x.During("Triggered").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new Trigger { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.Same(sendEvent, message.Message);
    }

    [Fact]
    public async Task Sage_Should_CallLifeCycleOnEntryPublish_When_Transition()
    {
        // Arrange
        var initState = new TestState();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => initState).TransitionTo("Started");

                x.During("Started").OnEntry().Publish(_ => new Publish());

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new Start());
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.IsType<Publish>(message.Message);
    }

    [Fact]
    public async Task Sage_Should_CallLifeCycleOnEntrySend_When_Transition()
    {
        // Arrange
        var initState = new TestState();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => initState).TransitionTo("Started");

                x.During("Started").OnEntry().Send(_ => new Send());

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        // Act
        var context = CreateContext(saga, new Start());
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.IsType<Send>(message.Message);
    }

    [Fact]
    public async Task Saga_Should_DeleteState_OnFinalEvent()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new End { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        Assert.Empty(_store.States);
    }

    [Fact(Skip = "Cleanup is currently disabled in Saga.OnEnterStateAsync - TODO re-enable")]
    public async Task Saga_Should_CleanupSagaState_OnFinalEvent()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new End { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_cleanup.CleanedStates);
        Assert.Equal(initState.Id, state.Id);
    }

    [Fact]
    public async Task Saga_Should_PublishEvent_OnFinalEvent()
    {
        // Arrange
        var publishEvent = new Publish();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended").Publish((_, _) => publishEvent);

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new End { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.Same(publishEvent, message.Message);
    }

    [Fact]
    public async Task Saga_Should_SendEvent_OnFinalEvent()
    {
        // Arrange
        var sendEvent = new Send();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended").Send((_, _) => sendEvent);

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new End { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.Same(sendEvent, message.Message);
    }

    [Fact]
    public async Task Saga_Should_Respond_When_EnterFinalState()
    {
        // Arrange
        var replyEvent = new Reply();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended").Respond(_ => replyEvent);
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        initState.Metadata.Set("correlation-id", initState.Id.ToString());
        initState.Metadata.Set("saga-reply-address", "http://test/reply");
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new End { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.Same(replyEvent, message.Message);
        Assert.Equal(new Uri("http://test/reply"), ((ReplyOptions)message.Options!).ReplyAddress);
        Assert.Equal(initState.Id.ToString(), ((ReplyOptions)message.Options!).CorrelationId);
    }

    [Fact]
    public async Task Saga_Should_NotFailOnRespond_When_FinalStateAndNoCorrelationId()
    {
        // Arrange
        var replyEvent = new Reply();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended").Respond(_ => replyEvent);
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new End { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        Assert.Empty(_outbox.Messages);
        Assert.Equal("Ended", initState.State);
    }

    [Theory]
    [InlineData("StateA")]
    [InlineData("StateB")]
    public async Task Saga_Should_Transition_DuringAny(string value)
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("StateA");

                x.DuringAny().OnEvent<AnyEvent>().TransitionTo("AnyTriggered");

                x.During("StateA").OnEvent<Trigger>().TransitionTo("StateB");

                x.During("StateB").OnEvent<Trigger>().TransitionTo("Ended");

                x.During("AnyTriggered").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = value };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new AnyEvent { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States);
        Assert.Equal("AnyTriggered", state.State);
    }

    [Theory]
    [InlineData("StateA")]
    [InlineData("StateB")]
    public async Task Saga_Should_PublishEvent_OnAny(string value)
    {
        // Arrange
        var publishEvent = new Publish();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("StateA");

                x.DuringAny().OnEvent<AnyEvent>().TransitionTo("AnyTriggered").Publish((_, _) => publishEvent);

                x.During("StateA").OnEvent<Trigger>().TransitionTo("StateB");

                x.During("StateB").OnEvent<Trigger>().TransitionTo("Ended");

                x.During("AnyTriggered").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = value };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new AnyEvent { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.Same(publishEvent, message.Message);
    }

    [Theory]
    [InlineData("StateA")]
    [InlineData("StateB")]
    public async Task Saga_Should_SendEvent_OnAny(string value)
    {
        // Arrange
        var sendEvent = new Send();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("StateA");

                x.DuringAny().OnEvent<AnyEvent>().TransitionTo("AnyTriggered").Send((_, _) => sendEvent);

                x.During("StateA").OnEvent<Trigger>().TransitionTo("StateB");

                x.During("StateB").OnEvent<Trigger>().TransitionTo("Ended");

                x.During("AnyTriggered").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended");
            });

        Initialize(saga);

        var initState = new TestState { State = value };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new AnyEvent { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.Same(sendEvent, message.Message);
    }

    [Fact]
    public async Task Sage_Should_CallLifeCycleOnEntryPublish_When_OnEntry()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended").OnEntry().Publish(_ => new Publish());
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new End { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.IsType<Publish>(message.Message);
    }

    [Fact]
    public async Task Sage_Should_CallLifeCycleOnEntrySend_When_OnEntry()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<Start>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<End>().TransitionTo("Ended");

                x.Finally("Ended").OnEntry().Send(_ => new Send());
            });

        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act
        var context = CreateContext(saga, new End { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.IsType<Send>(message.Message);
    }

    private TestConsumeContext CreateContext(Saga saga, object message)
    {
        var context = new TestConsumeContext
        {
            CancellationToken = CancellationToken.None,
            CorrelationId = Guid.NewGuid().ToString(),
            MessageId = Guid.NewGuid().ToString(),
            Services = _services,
            Runtime = _runtime
        };

        // Set the message via MessageParsingFeature so context.GetMessage() works
        var messageFeature = context.Features.GetOrSet<MessageParsingFeature>();
        messageFeature.Message = message;

        // Set the saga store via SagaFeature
        var sagaFeature = context.Features.GetOrSet<SagaFeature>();
        sagaFeature.Store = _store;

        return context;
    }

    private static void Initialize(Saga saga)
    {
        saga.Initialize(TestMessagingSetupContext.Instance);
    }

    private sealed class TestState : SagaStateBase
    {
        public List<string> History { get; set; } = [];
    }

    private sealed class Start;

    private sealed class Reply;

    private sealed class Trigger : ICorrelatable
    {
        public Guid? CorrelationId { get; init; }
    }

    private sealed class AnyEvent : ICorrelatable
    {
        public Guid? CorrelationId { get; init; }
    }

    private sealed class End : ICorrelatable
    {
        public Guid? CorrelationId { get; init; }
    }

    private sealed class Publish;

    private sealed class Send;
}
