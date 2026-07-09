using System.Buffers;

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
    public void Initialize_Should_AssignStateAndTransitionUrns_When_DuringAnyTransitionsExist()
    {
        // Arrange
        // Act
        var saga = CreateInitializedSagaWithCancelTransitions();
        var started = saga.States["Started"];
        var processing = saga.States["Processing"];
        var startedCancel = started.Transitions.Values.Single(t => t.EventType == typeof(CancelEvent));
        var processingCancel = processing.Transitions.Values.Single(t => t.EventType == typeof(CancelEvent));

        // Assert
        Assert.Equal(MochaUrn.SagaState(saga.Urn, "Started"), started.Urn);
        Assert.Equal(MochaUrn.SagaState(saga.Urn, "Processing"), processing.Urn);
        Assert.Equal(
            MochaUrn.SagaTransition(saga.Urn, "Started", nameof(CancelEvent)),
            startedCancel.Urn);
        Assert.Equal(
            MochaUrn.SagaTransition(saga.Urn, "Processing", nameof(CancelEvent)),
            processingCancel.Urn);
        Assert.NotEqual(startedCancel.Urn, processingCancel.Urn);
    }

    [Fact]
    public void Describe_Should_UseStateAndTransitionUrns_When_SagaInitialized()
    {
        // Arrange
        // Act
        var saga = CreateInitializedSagaWithCancelTransitions();
        var started = saga.States["Started"];
        var startedCancel = started.Transitions.Values.Single(t => t.EventType == typeof(CancelEvent));

        var description = saga.Describe();
        var startedDescription = description.States.Single(s => s.Name == "Started");
        var startedCancelDescription = startedDescription.Transitions.Single(t => t.EventType == nameof(CancelEvent));

        // Assert
        Assert.Equal(started.Urn, startedDescription.Id);
        Assert.Equal(startedCancel.Urn, startedCancelDescription.Id);
    }

    [Fact]
    public void Describe_Should_ReturnSource_When_SagaConfigurationHasSource()
    {
        // Arrange
        var source = new SourceMetadata
        {
            Assembly = "Mocha.Sagas.Tests",
            RepositoryUrl = "https://github.com/example/mocha",
            Commit = "abc123",
            XmlDocumentation = "<summary>Test saga.</summary>",
            DeclarationLocation = new DeclarationLocation("TestSaga.cs", 1, 1, 10, 2)
        };
        var saga = CreateInitializedSagaWithSource(source);

        // Act
        var description = saga.Describe();

        // Assert
        Assert.Same(source, description.Source);
    }

    [Fact]
    public void ConsumerDescribe_Should_FallBackToSagaSource_When_ConsumerConfigurationHasNoSource()
    {
        // Arrange - the saga consumer never sets Source on its own ConsumerConfiguration, so
        // Consumer.Describe() must fall back to the saga's own Source.
        var source = new SourceMetadata
        {
            Assembly = "Mocha.Sagas.Tests",
            RepositoryUrl = "https://github.com/example/mocha",
            Commit = "abc123",
            XmlDocumentation = "<summary>Test saga.</summary>",
            DeclarationLocation = new DeclarationLocation("TestSaga.cs", 1, 1, 10, 2)
        };
        var saga = CreateInitializedSagaWithSource(source);

        // Act
        var description = saga.Consumer.Describe();

        // Assert
        Assert.Same(source, description.Source);
    }

    private Saga<TestState> CreateInitializedSagaWithSource(SourceMetadata source)
    {
        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor.Extend().Configuration.Source = source;

                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(s => new TestState(Guid.NewGuid(), "Started"));
                descriptor.During("Started").OnEvent<TriggerEvent>().TransitionTo("Success");
                descriptor.Finally("Success");
            });

        saga.Initialize(_context);

        return saga;
    }

    private Saga<TestState> CreateInitializedSagaWithCancelTransitions()
    {
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

        return saga;
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

    [Fact]
    public void Initialize_Should_Succeed_When_ConfigurationFeatureIsMissing()
    {
        // Arrange
        var context = new TestMessagingSetupContext();
        context.Features.Set<MessagingConfigurationFeature>(null);

        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(_ => new TestState(Guid.NewGuid(), "Started"));

                descriptor.Finally("Started");
            });

        // Act
        saga.Initialize(context);

        // Assert
        Assert.Equal(2, saga.States.Count);
    }

    [Fact]
    public void Initialize_Should_KeepPresetStateSerializer_When_DescriptorHasNoSerializer()
    {
        // Arrange
        var preset = new StubSagaStateSerializer();

        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(_ => new TestState(Guid.NewGuid(), "Started"));

                descriptor.Finally("Started");
            });

        saga.StateSerializer = preset;

        // Act
        saga.Initialize(TestMessagingSetupContext.Instance);

        // Assert
        Assert.Same(preset, saga.StateSerializer);
    }

    [Fact]
    public void Initialize_Should_UseDescriptorSerializer_When_PresetSerializerExists()
    {
        // Arrange
        var fromDescriptor = new StubSagaStateSerializer();

        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Started")
                    .StateFactory(_ => new TestState(Guid.NewGuid(), "Started"));

                descriptor.Finally("Started");
                descriptor.Serializer(_ => fromDescriptor);
            });

        saga.StateSerializer = new StubSagaStateSerializer();

        // Act
        saga.Initialize(TestMessagingSetupContext.Instance);

        // Assert
        Assert.Same(fromDescriptor, saga.StateSerializer);
    }

    private sealed class StubSagaStateSerializer : ISagaStateSerializer
    {
        public T? Deserialize<T>(ReadOnlyMemory<byte> body) => default;

        public object? Deserialize(ReadOnlyMemory<byte> body) => null;

        public void Serialize<T>(T message, IBufferWriter<byte> writer) { }

        public void Serialize(object message, IBufferWriter<byte> writer) { }
    }

    private class TestState(Guid id, string state) : SagaStateBase(id, state);

    private sealed class StartEvent;

    private sealed class TriggerEvent;

    private sealed class CancelEvent;

    private sealed record TestMessage(Guid Id);
}
