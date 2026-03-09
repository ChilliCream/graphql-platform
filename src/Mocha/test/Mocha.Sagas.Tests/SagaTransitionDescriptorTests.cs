namespace Mocha.Sagas.Tests;

public class SagaTransitionDescriptorTests
{
    private static readonly IMessagingConfigurationContext s_context = TestMessagingSetupContext.Instance;

    [Fact]
    public void Then_ShouldSetAction_WhenCalled()
    {
        // Arrange
        var transitionDescriptor = new SagaTransitionDescriptor<TestState, string>(s_context, SagaTransitionKind.Event);
        Action<TestState, string> action = (state, evt) => { };

        // Act
        transitionDescriptor.Then(action);

        // Assert
        var definition = transitionDescriptor.CreateConfiguration();
        Assert.NotNull(definition.Action);
    }

    [Fact]
    public void TransitionTo_ShouldSetTransitionTo_WhenCalled()
    {
        // Arrange
        var transitionDescriptor = new SagaTransitionDescriptor<TestState, string>(s_context, SagaTransitionKind.Event);

        // Act
        transitionDescriptor.TransitionTo("NextState");

        // Assert
        var definition = transitionDescriptor.CreateConfiguration();
        Assert.Equal("NextState", definition.TransitionTo);
    }

    [Fact]
    public void Publish_ShouldAddNewPublishDefinition_WhenCalled()
    {
        // Arrange
        var transitionDescriptor = new SagaTransitionDescriptor<TestState, string>(s_context, SagaTransitionKind.Event);

        // Act
        transitionDescriptor.Publish((_, state) => new SagaTimedOutEvent(Guid.NewGuid()));

        // Assert
        var definition = transitionDescriptor.CreateConfiguration();
        Assert.Single(definition.Publish);
        Assert.Equal(typeof(SagaTimedOutEvent), definition.Publish[0].MessageType);
    }

    [Fact]
    public void StateFactory_ShouldSetFactory_WhenCalled()
    {
        // Arrange
        var transitionDescriptor = new SagaTransitionDescriptor<TestState, string>(s_context, SagaTransitionKind.Event);
        Func<string, TestState> factory = state => new TestState(Guid.NewGuid(), state);

        // Act
        transitionDescriptor.StateFactory(factory);

        // Assert
        Assert.NotNull(transitionDescriptor.Configuration.StateFactory);
    }

    public sealed class TestState(Guid id, string state) : SagaStateBase(id, state);
}
