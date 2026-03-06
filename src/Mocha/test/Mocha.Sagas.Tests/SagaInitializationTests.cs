namespace Mocha.Sagas.Tests;

public class SagaInitializationTests
{
    private readonly IMessagingSetupContext _context = TestMessagingSetupContext.Instance;

    [Fact]
    public void Initialize_Should_SetSagaNameBasedOnType()
    {
        // Arrange

        // Act
        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(s => new TestState(Guid.NewGuid(), "Started"));

                descriptor.During("Started").OnEvent<TriggerEvent>().TransitionTo("Success");

                descriptor.Finally("Success");
            });

        saga.Initialize(_context);

        // Assert - name is derived from type via naming conventions
        Assert.NotNull(saga.Name);
        Assert.NotEqual("__Unnamed", saga.Name);
    }

    [Fact]
    public void Initialize_Should_InitializeSagaStates()
    {
        // Arrange
        // Act
        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(s => new TestState(Guid.NewGuid(), "Started"));
                descriptor.During("Started").OnEvent<TriggerEvent>().TransitionTo("Success");
                descriptor.Finally("Success");
            });

        saga.Initialize(_context);

        // Assert
        Assert.Equal(3, saga.States.Count);
    }

    [Fact]
    public void Initialize_Should_InitializeInitialState()
    {
        // Arrange
        // Act
        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(s => new TestState(Guid.NewGuid(), "Started"));

                descriptor.During("Started").OnEvent<TriggerEvent>().TransitionTo("Success");
                descriptor.Finally("Success");
            });

        saga.Initialize(_context);

        // Assert
        Assert.True(saga.States["__Initial"].IsInitial);
        Assert.False(saga.States["__Initial"].IsFinal);
        var initial2Started = saga.States["__Initial"].Transitions[typeof(StartEvent)];
        Assert.Equal("Started", initial2Started.TransitionTo);
        Assert.Equal(typeof(StartEvent), initial2Started.EventType);
        Assert.Empty(initial2Started.Publish);
    }

    [Fact]
    public void Initialize_Should_InitializeDuringState()
    {
        // Arrange
        // Act
        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(s => new TestState(Guid.NewGuid(), "Started"));
                descriptor.During("Started").OnEvent<TriggerEvent>().TransitionTo("Success");
                descriptor.Finally("Success");
            });

        saga.Initialize(_context);

        // Assert
        Assert.False(saga.States["Started"].IsInitial);
        Assert.False(saga.States["Started"].IsFinal);
        var started2Success = saga.States["Started"].Transitions[typeof(TriggerEvent)];
        Assert.Equal("Success", started2Success.TransitionTo);
        Assert.Equal(typeof(TriggerEvent), started2Success.EventType);
        Assert.Empty(started2Success.Publish);
    }

    [Fact]
    public void Initialize_Should_InitializeFinal()
    {
        // Arrange
        // Act
        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(s => new TestState(Guid.NewGuid(), "Started"));
                descriptor.During("Started").OnEvent<TriggerEvent>().TransitionTo("Success");
                descriptor.Finally("Success");
            });

        saga.Initialize(_context);

        // Assert
        Assert.False(saga.States["Success"].IsInitial);
        Assert.True(saga.States["Success"].IsFinal);
        Assert.Empty(saga.States["Success"].Transitions);
    }

    [Fact]
    public void DuringAny_Should_AddTransitionToAllStates()
    {
        // Arrange
        // Act
        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(s => new TestState(Guid.NewGuid(), "Started"));
                descriptor.During("Started").OnEvent<TriggerEvent>().TransitionTo("Processing");
                descriptor.During("Processing").OnEvent<TriggerEvent>().TransitionTo("Success");
                descriptor.DuringAny().OnEvent<CancelEvent>().TransitionTo("Cancelled");
                descriptor.Finally("Success");
                descriptor.Finally("Cancelled");
            });

        saga.Initialize(_context);

        // Assert
        Assert.Equal(2, saga.States["Started"].Transitions.Count);
        var cancelTransition = saga.States["Started"]
            .Transitions.Values.Single(t => t.EventType == typeof(CancelEvent));
        Assert.Equal("Cancelled", cancelTransition.TransitionTo);
        Assert.Empty(cancelTransition.Publish);
        Assert.Equal(2, saga.States["Processing"].Transitions.Count);
        cancelTransition = saga.States["Processing"].Transitions.Values.Single(t => t.EventType == typeof(CancelEvent));
        Assert.Equal("Cancelled", cancelTransition.TransitionTo);
        Assert.Empty(cancelTransition.Publish);
    }

    [Fact]
    public void DuringAny_Should_NotBeAddedToFinalState()
    {
        // Arrange
        // Act
        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(s => new TestState(Guid.NewGuid(), "Started"));
                descriptor.During("Started").OnEvent<TriggerEvent>().TransitionTo("Processing");
                descriptor.During("Processing").OnEvent<TriggerEvent>().TransitionTo("Success");
                descriptor.DuringAny().OnEvent<CancelEvent>().TransitionTo("Cancelled");
                descriptor.Finally("Success");
                descriptor.Finally("Cancelled");
            });

        saga.Initialize(_context);

        // Assert
        Assert.Empty(saga.States["Success"].Transitions);
    }

    [Fact]
    public void DuringAny_Should_NotBeAddedToInitialStates()
    {
        // Arrange
        // Act
        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(s => new TestState(Guid.NewGuid(), "Started"));
                descriptor.During("Started").OnEvent<TriggerEvent>().TransitionTo("Processing");
                descriptor.During("Processing").OnEvent<TriggerEvent>().TransitionTo("Success");
                descriptor.DuringAny().OnEvent<CancelEvent>().TransitionTo("Cancelled");
                descriptor.Finally("Success");
                descriptor.Finally("Cancelled");
            });

        saga.Initialize(_context);

        // Assert
        var initial2Started = Assert.Single(saga.States["__Initial"].Transitions.Values);
        Assert.Equal("Started", initial2Started.TransitionTo);
        Assert.Equal(typeof(StartEvent), initial2Started.EventType);
        Assert.Empty(saga.States["__Initial"].Transitions[typeof(StartEvent)].Publish);
    }

    [Fact]
    public void Transition_Should_RequireTransitionTo()
    {
        // Arrange
        // Act
        var exception =
            Assert.Throws<SagaInitializationException>(() =>
            {
                var saga =
                    Saga.Create<TestState>(descriptor =>
                        descriptor.Initially().OnEvent<StartEvent>().Then((_, _) => { }));
                saga.Initialize(_context);
            });

        // Assert
        Assert.Equal("Transition target state is not defined.", exception.Message);
    }

    [Fact]
    public void Publish_Should_BeInitialized()
    {
        // Arrange
        // Act
        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(_ => new TestState(Guid.NewGuid(), "Started"))
                    .Publish(_ => new TestMessage(Guid.NewGuid()));

                descriptor.During("Started").OnEvent<TriggerEvent>().TransitionTo("Success");

                descriptor.Finally("Success");
            });

        saga.Initialize(_context);

        // Assert
        var initial2Started = Assert.Single(saga.States["__Initial"].Transitions.Values);
        Assert.Equal("Started", initial2Started.TransitionTo);
        Assert.Equal(typeof(StartEvent), initial2Started.EventType);
        var publish = Assert.Single(initial2Started.Publish);
        Assert.Equal(typeof(TestMessage), publish.MessageType);
    }

    [Fact]
    public void Initially_Must_DefineStateFactory()
    {
        // Arrange
        // Act
        var exception =
            Assert.Throws<SagaInitializationException>(() =>
            {
                var saga =
                    Saga.Create<TestState>(descriptor =>
                        descriptor.Initially().OnEvent<StartEvent>().TransitionTo("Started"));
                saga.Initialize(_context);
            });

        // Assert
        Assert.Equal("When 'StartEvent' is triggered, no state factory is defined.", exception.Message);
    }

    private class TestState(Guid id, string state) : SagaStateBase(id, state);

    private sealed class StartEvent;

    private sealed class TriggerEvent;

    private sealed class CancelEvent;

    private sealed record TestMessage(Guid Id);
}
