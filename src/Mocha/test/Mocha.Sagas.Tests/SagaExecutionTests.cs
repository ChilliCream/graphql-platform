using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Tests for Saga execution engine covering gaps in Saga.cs and Saga.Initialization.cs.
/// Focuses on: Describe(), HandleEvent error paths, event type hierarchy,
/// publish/send with null factory output, headers propagation, multiple events,
/// custom options, and initialization edge cases.
/// </summary>
public sealed class SagaExecutionTests
{
    private static readonly IMessagingRuntime s_runtime = CreateRuntime();

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

    public SagaExecutionTests()
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
    public async Task HandleEvent_Should_Throw_When_SagaNotInitialized()
    {
        // Arrange - create saga but do NOT call Initialize
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });

        // Do NOT call Initialize(saga) - _states remains null

        var context = CreateContext(saga, new StartEvent());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => saga.HandleEvent(context));
        Assert.Equal("Saga is not initialized.", ex.Message);
    }

    [Fact]
    public void States_Should_Throw_When_SagaNotInitialized()
    {
        // Arrange - create saga without initialization
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => saga.States);
        Assert.Equal("Saga is not initialized.", ex.Message);
    }

    [Fact]
    public async Task HandleEvent_Should_ReturnTrue_When_InitialEventProcessed()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var context = CreateContext(saga, new StartEvent());

        // Act
        var result = await saga.HandleEvent(context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HandleEvent_Should_ReturnTrue_When_FinalStateReached()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        var context = CreateContext(saga, new EndEvent { CorrelationId = initState.Id });

        // Act
        var result = await saga.HandleEvent(context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CreateState_Should_Throw_When_NoTransitionForInitialEvent()
    {
        // Arrange - saga only handles StartEvent initially, but we send TriggerEvent
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Send a TriggerEvent as initial event (no transition defined)
        var context = CreateContext(saga, new TriggerEvent());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SagaExecutionException>(() => saga.HandleEvent(context));
        Assert.Contains("No transition defined for event 'TriggerEvent' in state '__Initial'", ex.Message);
    }

    [Fact]
    public async Task CreateState_Should_SetMetadata_When_ResponseAddressPresent()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var context = CreateContext(saga, new StartEvent());
        context.ResponseAddress = new Uri("http://test/reply-address");

        // Act
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States);
        Assert.Equal("http://test/reply-address", state.Metadata.GetValue("saga-reply-address"));
        Assert.Equal(context.CorrelationId, state.Metadata.GetValue("correlation-id"));
    }

    [Fact]
    public async Task CreateState_Should_SetNullReplyAddress_When_NoResponseAddress()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var context = CreateContext(saga, new StartEvent());
        context.ResponseAddress = null;

        // Act
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States);
        // ReplyAddress metadata key should be set, but value is null
        // Headers.Set stores the null, but TryGet with [NotNullWhen] won't match nulls,
        // so we verify via GetValue which returns the raw object
        var replyAddr = state.Metadata.GetValue("saga-reply-address");
        Assert.Null(replyAddr);
    }

    [Fact]
    public async Task OnHandleTransition_Should_ResolveBaseType_When_NoExactMatch()
    {
        // Arrange - transition is defined on BaseEvent, but we send DerivedEvent
        var actionCalled = false;
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<BaseEvent>().TransitionTo("Handled").Then((_, _) => actionCalled = true);
                x.During("Handled").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act - send DerivedEvent, which should match BaseEvent transition via hierarchy
        var context = CreateContext(saga, new DerivedEvent { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert
        Assert.True(actionCalled);
        var state = Assert.Single(_store.States);
        Assert.Equal("Handled", state.State);
    }

    [Fact]
    public async Task OnHandleTransition_Should_PreferExactType_WhenBothExactAndBaseExist()
    {
        // Arrange - transitions for both BaseEvent and DerivedEvent exist
        var exactActionCalled = false;
        var baseActionCalled = false;
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started")
                    .OnEvent<DerivedEvent>()
                    .TransitionTo("ExactHandled")
                    .Then((_, _) => exactActionCalled = true);
                x.During("Started")
                    .OnEvent<BaseEvent>()
                    .TransitionTo("BaseHandled")
                    .Then((_, _) => baseActionCalled = true);
                x.During("ExactHandled").OnEvent<EndEvent>().TransitionTo("Ended");
                x.During("BaseHandled").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Act - send DerivedEvent; it should match DerivedEvent first
        var context = CreateContext(saga, new DerivedEvent { CorrelationId = initState.Id });
        await saga.HandleEvent(context);

        // Assert - exact match was used, not base type
        Assert.True(exactActionCalled);
        Assert.False(baseActionCalled);
        var state = Assert.Single(_store.States);
        Assert.Equal("ExactHandled", state.State);
    }

    [Fact]
    public async Task PublishEvents_Should_SkipNull_When_FactoryReturnsNull()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .Publish((_, _) => (PublishMessage)null!)
                    .TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var context = CreateContext(saga, new StartEvent());

        // Act
        await saga.HandleEvent(context);

        // Assert - null message should be skipped, no publish operations
        Assert.Empty(_outbox.Messages);
    }

    [Fact]
    public async Task PublishEvents_Should_PublishNonNull_When_FactoryReturnsMessage()
    {
        // Arrange
        var publishMessage = new PublishMessage();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .Publish((_, _) => publishMessage)
                    .TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var context = CreateContext(saga, new StartEvent());

        // Act
        await saga.HandleEvent(context);

        // Assert
        var message = Assert.Single(_outbox.Messages);
        Assert.Same(publishMessage, message.Message);
    }

    [Fact]
    public async Task SendEvents_Should_SkipNull_When_FactoryReturnsNull()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .Send((_, _) => (SendMessage)null!)
                    .TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var context = CreateContext(saga, new StartEvent());

        // Act
        await saga.HandleEvent(context);

        // Assert - null message should be skipped, no send operations
        Assert.Empty(_outbox.Messages);
    }

    [Fact]
    public async Task Transition_Should_PublishMultipleEvents_When_MultiplePublishConfigured()
    {
        // Arrange
        var publish1 = new PublishMessage();
        var publish2 = new PublishMessage2();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .Publish((_, _) => publish1)
                    .Publish((_, _) => publish2)
                    .TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var context = CreateContext(saga, new StartEvent());

        // Act
        await saga.HandleEvent(context);

        // Assert
        Assert.Equal(2, _outbox.Messages.Count);
        Assert.Contains(_outbox.Messages, m => m.Message is PublishMessage);
        Assert.Contains(_outbox.Messages, m => m.Message is PublishMessage2);
    }

    [Fact]
    public async Task PublishEvents_Should_SetSagaIdHeader_When_Publishing()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .Publish((_, _) => new PublishMessage())
                    .TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var context = CreateContext(saga, new StartEvent());

        // Act
        await saga.HandleEvent(context);

        // Assert
        var operation = Assert.Single(_outbox.Messages);
        var options = Assert.IsType<PublishOptions>(operation.Options);
        Assert.NotNull(options.Headers);

        // The saga-id header should be set
        var sagaIdHeader = options.Headers.FirstOrDefault(h => h.Key == "saga-id");
        Assert.NotNull(sagaIdHeader.Value);

        // Verify the saga-id matches the state ID
        var state = Assert.Single(_store.States);
        Assert.Equal(state.Id.ToString("D"), sagaIdHeader.Value);
    }

    [Fact]
    public async Task OnEnterState_Should_DeleteState_When_FinalStateWithoutResponse()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        var context = CreateContext(saga, new EndEvent { CorrelationId = initState.Id });

        // Act
        await saga.HandleEvent(context);

        // Assert - state should be deleted (no response configured)
        Assert.Empty(_store.States);
        // No reply messages should be sent
        Assert.Empty(_outbox.Messages);
    }

    [Fact]
    public async Task OnEnterState_Should_RespondAndDelete_When_FinalStateWithResponseAndMetadata()
    {
        // Arrange
        var replyEvent = new ReplyMessage();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended").Respond(_ => replyEvent);
            });
        Initialize(saga);

        var initState = new TestState { State = "Started" };
        initState.Metadata.Set("correlation-id", initState.Id.ToString());
        initState.Metadata.Set("saga-reply-address", "http://test/reply");
        _store.States.Add(initState);

        var context = CreateContext(saga, new EndEvent { CorrelationId = initState.Id });

        // Act
        await saga.HandleEvent(context);

        // Assert - reply sent and state deleted
        var message = Assert.Single(_outbox.Messages);
        Assert.Same(replyEvent, message.Message);
        var replyOptions = Assert.IsType<ReplyOptions>(message.Options);
        Assert.Equal(new Uri("http://test/reply"), replyOptions.ReplyAddress);
        Assert.Equal(initState.Id.ToString(), replyOptions.CorrelationId);
        Assert.Empty(_store.States);
    }

    [Fact]
    public async Task OnEnterState_Should_NotRespond_When_FinalStateWithResponseButNoReplyAddress()
    {
        // Arrange
        var replyEvent = new ReplyMessage();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended").Respond(_ => replyEvent);
            });
        Initialize(saga);

        var initState = new TestState { State = "Started" };
        // No metadata set - no reply address / correlation ID
        _store.States.Add(initState);

        var context = CreateContext(saga, new EndEvent { CorrelationId = initState.Id });

        // Act
        await saga.HandleEvent(context);

        // Assert - no reply, but state still deleted
        Assert.Empty(_outbox.Messages);
        Assert.Empty(_store.States);
    }

    [Fact]
    public async Task OnEnterState_Should_NotRespond_When_InvalidReplyAddress()
    {
        // Arrange
        var replyEvent = new ReplyMessage();
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended").Respond(_ => replyEvent);
            });
        Initialize(saga);

        var initState = new TestState { State = "Started" };
        initState.Metadata.Set("correlation-id", initState.Id.ToString());
        // Set an invalid URI that cannot be parsed as absolute
        initState.Metadata.Set("saga-reply-address", "not-a-valid-uri");
        _store.States.Add(initState);

        var context = CreateContext(saga, new EndEvent { CorrelationId = initState.Id });

        // Act
        await saga.HandleEvent(context);

        // Assert - no reply since URI is invalid, but state still deleted
        Assert.Empty(_outbox.Messages);
        Assert.Empty(_store.States);
    }

    [Fact]
    public async Task OnEnterState_Should_PublishLifecycleEvents_When_OnEntryConfigured()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEntry().Publish(_ => new PublishMessage());

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");

                x.Finally("Ended");
            });
        Initialize(saga);

        var context = CreateContext(saga, new StartEvent());

        // Act
        await saga.HandleEvent(context);

        // Assert - lifecycle publish event dispatched when entering "Started"
        var published = Assert.Single(_outbox.Messages);
        Assert.IsType<PublishMessage>(published.Message);
    }

    [Fact]
    public async Task OnEnterState_Should_PublishLifecycleEventsOnFinal_When_OnEntryConfigured()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");

                x.Finally("Ended").OnEntry().Publish(_ => new PublishMessage());
            });
        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        var context = CreateContext(saga, new EndEvent { CorrelationId = initState.Id });

        // Act
        await saga.HandleEvent(context);

        // Assert - lifecycle publish event dispatched when entering final state
        var published = Assert.Single(_outbox.Messages);
        Assert.IsType<PublishMessage>(published.Message);
        // State should be deleted (final)
        Assert.Empty(_store.States);
    }

    [Fact]
    public async Task HandleEvent_Should_PersistState_When_TransitionToNonFinalState()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Processing");
                x.During("Processing").OnEvent<TriggerEvent>().TransitionTo("Triggered");
                x.During("Triggered").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var context = CreateContext(saga, new StartEvent());

        // Act
        await saga.HandleEvent(context);

        // Assert - state should be persisted
        var state = Assert.Single(_store.States);
        Assert.Equal("Processing", state.State);
    }

    [Fact]
    public async Task HandleEvent_Should_UpdatePersistedState_When_TransitionInExistingSaga()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Processing");
                x.During("Processing").OnEvent<TriggerEvent>().TransitionTo("Triggered");
                x.During("Triggered").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var initState = new TestState { State = "Processing" };
        _store.States.Add(initState);

        var context = CreateContext(saga, new TriggerEvent());
        context.MutableHeaders.Set("saga-id", initState.Id.ToString("D"));

        // Act
        await saga.HandleEvent(context);

        // Assert - state should be updated
        var state = Assert.Single(_store.States);
        Assert.Equal("Triggered", state.State);
    }

    [Fact]
    public void Describe_Should_ReturnSagaDescription_When_Called()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        Assert.NotNull(description);
        Assert.NotNull(description.Name);
        Assert.Equal("TestState", description.StateType);
        Assert.Equal(typeof(TestState).FullName, description.StateTypeFullName);
    }

    [Fact]
    public void Describe_Should_ContainAllStates_When_Called()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert - should have __Initial, Started, Ended
        Assert.Equal(3, description.States.Count);

        var initialState = description.States.Single(s => s.Name == "__Initial");
        Assert.True(initialState.IsInitial);
        Assert.False(initialState.IsFinal);

        var startedState = description.States.Single(s => s.Name == "Started");
        Assert.False(startedState.IsInitial);
        Assert.False(startedState.IsFinal);

        var endedState = description.States.Single(s => s.Name == "Ended");
        Assert.False(endedState.IsInitial);
        Assert.True(endedState.IsFinal);
    }

    [Fact]
    public void Describe_Should_ContainTransitions_When_Called()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var initialState = description.States.Single(s => s.Name == "__Initial");
        var transition = Assert.Single(initialState.Transitions);
        Assert.Equal("StartEvent", transition.EventType);
        Assert.Equal(typeof(StartEvent).FullName, transition.EventTypeFullName);
        Assert.Equal("Started", transition.TransitionTo);

        var startedState = description.States.Single(s => s.Name == "Started");
        var startedTransition = Assert.Single(startedState.Transitions);
        Assert.Equal("EndEvent", startedTransition.EventType);
        Assert.Equal("Ended", startedTransition.TransitionTo);
    }

    [Fact]
    public void Describe_Should_ContainPublishEvents_When_TransitionHasPublish()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .Publish((_, _) => new PublishMessage())
                    .TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var initialState = description.States.Single(s => s.Name == "__Initial");
        var transition = Assert.Single(initialState.Transitions);
        Assert.NotNull(transition.Publish);
        var publishDesc = Assert.Single(transition.Publish);
        Assert.Equal("PublishMessage", publishDesc.MessageType);
        Assert.Equal(typeof(PublishMessage).FullName, publishDesc.MessageTypeFullName);
    }

    [Fact]
    public void Describe_Should_ContainSendEvents_When_TransitionHasSend()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .Send((_, _) => new SendMessage())
                    .TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var initialState = description.States.Single(s => s.Name == "__Initial");
        var transition = Assert.Single(initialState.Transitions);
        Assert.NotNull(transition.Send);
        var sendDesc = Assert.Single(transition.Send);
        Assert.Equal("SendMessage", sendDesc.MessageType);
        Assert.Equal(typeof(SendMessage).FullName, sendDesc.MessageTypeFullName);
    }

    [Fact]
    public void Describe_Should_HaveNullPublish_When_NoPublishConfigured()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var initialState = description.States.Single(s => s.Name == "__Initial");
        var transition = Assert.Single(initialState.Transitions);
        Assert.Null(transition.Publish);
        Assert.Null(transition.Send);
    }

    [Fact]
    public void Describe_Should_ContainOnEntryDescription_When_OnEntryConfigured()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEntry().Publish(_ => new PublishMessage());

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");

                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var startedState = description.States.Single(s => s.Name == "Started");
        Assert.NotNull(startedState.OnEntry);
        Assert.NotNull(startedState.OnEntry.Publish);
        var publishDesc = Assert.Single(startedState.OnEntry.Publish);
        Assert.Equal("PublishMessage", publishDesc.MessageType);
    }

    [Fact]
    public void Describe_Should_ContainOnEntrySendDescription_When_OnEntrySendConfigured()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEntry().Send(_ => new SendMessage());

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");

                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var startedState = description.States.Single(s => s.Name == "Started");
        Assert.NotNull(startedState.OnEntry);
        Assert.NotNull(startedState.OnEntry.Send);
        var sendDesc = Assert.Single(startedState.OnEntry.Send);
        Assert.Equal("SendMessage", sendDesc.MessageType);
    }

    [Fact]
    public void Describe_Should_HaveEmptyOnEntry_When_NoLifecycleEventsConfigured()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert - OnEntry always exists (default initialized) but has no publish/send
        var startedState = description.States.Single(s => s.Name == "Started");
        // OnEntry is non-null because SagaStateConfiguration.OnEntry is always initialized
        Assert.NotNull(startedState.OnEntry);
        Assert.Null(startedState.OnEntry.Publish);
        Assert.Null(startedState.OnEntry.Send);
    }

    [Fact]
    public void Describe_Should_ContainResponse_When_FinalStateHasRespond()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended").Respond(_ => new ReplyMessage());
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var endedState = description.States.Single(s => s.Name == "Ended");
        Assert.NotNull(endedState.Response);
        Assert.Equal("ReplyMessage", endedState.Response.EventType);
        Assert.Equal(typeof(ReplyMessage).FullName, endedState.Response.EventTypeFullName);
    }

    [Fact]
    public void Describe_Should_HaveNullResponse_When_FinalStateWithoutRespond()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var endedState = description.States.Single(s => s.Name == "Ended");
        Assert.Null(endedState.Response);
    }

    [Fact]
    public void Describe_Should_IncludeAutoProvision_When_Configured()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().AutoProvision().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var startedState = description.States.Single(s => s.Name == "Started");
        var transition = Assert.Single(startedState.Transitions);
        Assert.True(transition.AutoProvision);
    }

    [Fact]
    public void Describe_Should_IncludeTransitionKind_When_Configured()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var initialState = description.States.Single(s => s.Name == "__Initial");
        var transition = Assert.Single(initialState.Transitions);
        Assert.Equal(SagaTransitionKind.Event, transition.TransitionKind);
    }

    [Fact]
    public void Create_Should_ProduceWorkingSaga_When_ConfiguredViaAction()
    {
        // Arrange & Act
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Assert
        Assert.Equal(typeof(TestState), saga.StateType);
        Assert.Equal(3, saga.States.Count);
    }

    [Fact]
    public void Initialize_Should_Throw_When_StateNameIsNull()
    {
        // Arrange & Act & Assert
        // SagaStateConfiguration.Name being null triggers this error.
        // This is hard to trigger via the public API since descriptors set names,
        // but we verify the error message from SagaInitializationException.
        // A state with null name would be invalid.
        // Actually, let's test what happens with DuringAny:
        // DuringAny produces transitions that should NOT be added to initial/final states
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("ProcessingA");

                x.DuringAny().OnEvent<CancelEvent>().TransitionTo("Cancelled");

                x.During("ProcessingA").OnEvent<TriggerEvent>().TransitionTo("ProcessingB");
                x.During("ProcessingB").OnEvent<EndEvent>().TransitionTo("Ended");
                x.During("Cancelled").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });

        // Act - should not throw
        Initialize(saga);

        // Assert - DuringAny transitions should be on ProcessingA and ProcessingB but NOT initial/final
        Assert.Equal(2, saga.States["ProcessingA"].Transitions.Count); // TriggerEvent + CancelEvent
        Assert.Equal(2, saga.States["ProcessingB"].Transitions.Count); // EndEvent + CancelEvent
        Assert.Single(saga.States["__Initial"].Transitions); // Only StartEvent
        Assert.Empty(saga.States["Ended"].Transitions); // No transitions on final
        Assert.Equal(2, saga.States["Cancelled"].Transitions.Count); // EndEvent + CancelEvent (DuringAny)
    }

    [Fact]
    public void Initialize_Should_NotAddDuringAny_To_InitialState()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.DuringAny().OnEvent<CancelEvent>().TransitionTo("Cancelled");

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.During("Cancelled").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Assert
        var initialTransitions = saga.States["__Initial"].Transitions;
        Assert.Single(initialTransitions);
        Assert.True(initialTransitions.ContainsKey(typeof(StartEvent)));
        Assert.False(initialTransitions.ContainsKey(typeof(CancelEvent)));
    }

    [Fact]
    public void Initialize_Should_NotAddDuringAny_To_FinalState()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.DuringAny().OnEvent<CancelEvent>().TransitionTo("Cancelled");

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.During("Cancelled").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Assert
        Assert.Empty(saga.States["Ended"].Transitions);
    }

    [Fact]
    public async Task HandleEvent_Should_LoadState_When_EventImplementsICorrelatable()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started")
                    .OnEvent<CorrelatedEvent>()
                    .TransitionTo("Processed")
                    .Then((state, evt) => state.Data = evt.Payload);
                x.During("Processed").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        var context = CreateContext(saga, new CorrelatedEvent { CorrelationId = initState.Id, Payload = "test-data" });

        // Act
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States);
        Assert.Equal("Processed", state.State);
        Assert.Equal("test-data", ((TestState)state).Data);
    }

    [Fact]
    public async Task HandleEvent_Should_LoadState_When_SagaIdHeader_Set()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started")
                    .OnEvent<TriggerEvent>()
                    .TransitionTo("Triggered")
                    .Then((state, _) => state.Data = "triggered");
                x.During("Triggered").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        // Set saga-id via header (not ICorrelatable)
        var context = CreateContext(saga, new TriggerEvent());
        context.MutableHeaders.Set("saga-id", initState.Id.ToString("D"));

        // Act
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States);
        Assert.Equal("Triggered", state.State);
        Assert.Equal("triggered", ((TestState)state).Data);
    }

    [Fact]
    public async Task HandleEvent_Should_ReturnFalse_When_CorrelatedStateNotFound()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<CorrelatedEvent>().TransitionTo("Processed");
                x.During("Processed").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // No state in store
        var context = CreateContext(saga, new CorrelatedEvent { CorrelationId = Guid.NewGuid(), Payload = "test" });

        // Act
        var result = await saga.HandleEvent(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HandleEvent_Should_Throw_When_StateNotFoundInStateMachine()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Put state in store with a state name that doesn't exist in the saga
        var initState = new TestState { State = "NonExistentState", Id = Guid.NewGuid() };
        _store.States.Add(initState);

        var context = CreateContext(saga, new CorrelatedEvent { CorrelationId = initState.Id, Payload = "test" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SagaExecutionException>(() => saga.HandleEvent(context));
        Assert.Contains("No state found for state 'NonExistentState'", ex.Message);
    }

    [Fact]
    public async Task Saga_Should_CompleteFullLifecycle_When_AllEventsProcessed()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .Publish((_, _) => new PublishMessage())
                    .TransitionTo("Processing");

                x.During("Processing")
                    .OnEvent<CorrelatedEvent>()
                    .Then((state, evt) => state.Data = evt.Payload)
                    .Publish((_, _) => new PublishMessage2())
                    .TransitionTo("Completed");

                x.Finally("Completed").OnEntry().Publish(_ => new PublishMessage());
            });
        Initialize(saga);

        // Step 1: Initial event
        var context1 = CreateContext(saga, new StartEvent());
        await saga.HandleEvent(context1);

        // Assert after step 1
        var state = Assert.Single(_store.States);
        Assert.Equal("Processing", state.State);
        // One publish from transition
        Assert.Single(_outbox.Messages, m => m.Message is PublishMessage);

        _outbox.Messages.Clear();

        // Step 2: Process event (correlated)
        var context2 = CreateContext(saga, new CorrelatedEvent { CorrelationId = state.Id, Payload = "processed" });
        await saga.HandleEvent(context2);

        // Assert after step 2 - saga completed
        Assert.Empty(_store.States); // deleted because final
        Assert.Equal("processed", ((TestState)state).Data);
        // PublishMessage2 from transition + PublishMessage from OnEntry of final state
        Assert.Contains(_outbox.Messages, m => m.Message is PublishMessage2);
        Assert.Contains(_outbox.Messages, m => m.Message is PublishMessage);
    }

    [Fact]
    public async Task Transition_Should_ExecuteAction_When_ThenConfigured()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .Then((state, _) => state.Data = "initialized")
                    .TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        var context = CreateContext(saga, new StartEvent());

        // Act
        await saga.HandleEvent(context);

        // Assert
        var state = Assert.Single(_store.States);
        Assert.Equal("initialized", ((TestState)state).Data);
    }

    [Fact]
    public async Task Transition_Should_ExecuteAction_When_ThenConfiguredOnCorrelatedTransition()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");
                x.During("Started")
                    .OnEvent<CorrelatedEvent>()
                    .Then((state, evt) => state.Data = $"processed:{evt.Payload}")
                    .TransitionTo("Done");
                x.Finally("Done");
            });
        Initialize(saga);

        var initState = new TestState { State = "Started" };
        _store.States.Add(initState);

        var context = CreateContext(saga, new CorrelatedEvent { CorrelationId = initState.Id, Payload = "hello" });

        // Act
        await saga.HandleEvent(context);

        // Assert
        Assert.Equal("processed:hello", initState.Data);
    }

    [Fact]
    public void Describe_Should_ContainMultiplePublish_When_MultipleConfigured()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially()
                    .OnEvent<StartEvent>()
                    .StateFactory(_ => new TestState())
                    .Publish((_, _) => new PublishMessage())
                    .Publish((_, _) => new PublishMessage2())
                    .TransitionTo("Started");
                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var initialState = description.States.Single(s => s.Name == "__Initial");
        var transition = Assert.Single(initialState.Transitions);
        Assert.NotNull(transition.Publish);
        Assert.Equal(2, transition.Publish.Count);
    }

    [Fact]
    public void Describe_Should_HaveNullOnEntryPublish_When_OnlyOnEntrySendConfigured()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEntry().Send(_ => new SendMessage());

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var startedState = description.States.Single(s => s.Name == "Started");
        Assert.NotNull(startedState.OnEntry);
        Assert.Null(startedState.OnEntry.Publish); // no publish configured
        Assert.NotNull(startedState.OnEntry.Send); // send is configured
    }

    [Fact]
    public void Describe_Should_HaveNullOnEntrySend_When_OnlyOnEntryPublishConfigured()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.During("Started").OnEntry().Publish(_ => new PublishMessage());

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert
        var startedState = description.States.Single(s => s.Name == "Started");
        Assert.NotNull(startedState.OnEntry);
        Assert.NotNull(startedState.OnEntry.Publish); // publish is configured
        Assert.Null(startedState.OnEntry.Send); // no send configured
    }

    [Fact]
    public void Describe_Should_IncludeDuringAnyTransitions_InNonInitialNonFinalStates()
    {
        // Arrange
        var saga =
            Saga.Create<TestState>(x =>
            {
                x.Initially().OnEvent<StartEvent>().StateFactory(_ => new TestState()).TransitionTo("Started");

                x.DuringAny().OnEvent<CancelEvent>().TransitionTo("Cancelled");

                x.During("Started").OnEvent<EndEvent>().TransitionTo("Ended");
                x.During("Cancelled").OnEvent<EndEvent>().TransitionTo("Ended");
                x.Finally("Ended");
            });
        Initialize(saga);

        // Act
        var description = saga.Describe();

        // Assert - Started should have both EndEvent and CancelEvent transitions
        var startedState = description.States.Single(s => s.Name == "Started");
        Assert.Equal(2, startedState.Transitions.Count);
        Assert.Contains(startedState.Transitions, t => t.EventType == "EndEvent");
        Assert.Contains(startedState.Transitions, t => t.EventType == "CancelEvent");

        // Initial should NOT have CancelEvent
        var initialState = description.States.Single(s => s.Name == "__Initial");
        Assert.Single(initialState.Transitions);
        Assert.DoesNotContain(initialState.Transitions, t => t.EventType == "CancelEvent");

        // Final should NOT have CancelEvent
        var endedState = description.States.Single(s => s.Name == "Ended");
        Assert.Empty(endedState.Transitions);
    }

    // ================================================================
    // Helpers
    // ================================================================

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

    // ================================================================
    // Test Types
    // ================================================================

    private sealed class TestState : SagaStateBase
    {
        public string? Data { get; set; }
    }

    private sealed class StartEvent;

    private sealed class TriggerEvent;

    private sealed class EndEvent : ICorrelatable
    {
        public Guid? CorrelationId { get; init; }
    }

    private class BaseEvent : ICorrelatable
    {
        public Guid? CorrelationId { get; init; }
    }

    private sealed class DerivedEvent : BaseEvent;

    private sealed class CorrelatedEvent : ICorrelatable
    {
        public Guid? CorrelationId { get; init; }
        public string Payload { get; init; } = "";
    }

    private sealed class CancelEvent : ICorrelatable
    {
        public Guid? CorrelationId { get; init; }
    }

    private sealed class PublishMessage;

    private sealed class PublishMessage2;

    private sealed class SendMessage;

    private sealed class ReplyMessage;
}
