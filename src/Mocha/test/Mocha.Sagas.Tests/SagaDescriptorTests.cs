namespace Mocha.Sagas.Tests;

public class SagaDescriptorTests
{
    private static readonly IMessagingConfigurationContext s_context = TestMessagingSetupContext.Instance;

    [Fact]
    public void Name_ShouldSetSagaName()
    {
        // Arrange
        var sagaDescriptor = new SagaDescriptor<TestState>(s_context);

        // Act
        sagaDescriptor.Name("TestSaga");

        // Assert
        Assert.Equal("TestSaga", sagaDescriptor.CreateConfiguration().Name);
    }

    [Fact]
    public void Initially_ShouldSetInitialState_WhenNoStateExists()
    {
        // Arrange
        var sagaDescriptor = new SagaDescriptor<TestState>(s_context);

        // Act
        sagaDescriptor.Initially().OnEvent<string>();

        // Assert
        var definition = sagaDescriptor.CreateConfiguration();
        var initState = Assert.Single(definition.States, x => x.IsInitial);
        Assert.Equal(typeof(string), initState.Transitions[0].EventType);
    }

    [Fact]
    public void Initially_ShouldReturnExistingInitialState_WhenAlreadyExists()
    {
        // Arrange
        var sagaDescriptor = new SagaDescriptor<TestState>(s_context);
        var initialState = sagaDescriptor.Initially();

        // Act
        var retrievedState = sagaDescriptor.Initially();

        // Assert
        Assert.Same(initialState, retrievedState);
    }

    [Fact]
    public void During_ShouldAddNewState_WhenStateDoesNotExist()
    {
        // Arrange
        var sagaDescriptor = new SagaDescriptor<TestState>(s_context);

        // Act
        sagaDescriptor.During("TestState");

        // Assert
        var definition = sagaDescriptor.CreateConfiguration();
        var stateDefinition = Assert.Single(definition.States);
        Assert.Equal("TestState", stateDefinition.Name);
        Assert.Single(sagaDescriptor.CreateConfiguration().States);
    }

    [Fact]
    public void During_ShouldReturnExistingState_WhenStateAlreadyExists()
    {
        // Arrange
        var sagaDescriptor = new SagaDescriptor<TestState>(s_context);
        var existingState = sagaDescriptor.During("TestState");

        // Act
        var retrievedState = sagaDescriptor.During("TestState");

        // Assert
        Assert.Same(existingState, retrievedState);
    }

    [Fact]
    public void DuringAny_ShouldAddNewStateWithNullName_WhenNoneExists()
    {
        // Arrange
        var sagaDescriptor = new SagaDescriptor<TestState>(s_context);

        // Act
        sagaDescriptor.DuringAny().OnEvent<string>();

        // Assert
        var definition = sagaDescriptor.CreateConfiguration();
        Assert.Empty(definition.States);
        Assert.NotNull(definition.DuringAny);
        Assert.Equal(typeof(string), definition.DuringAny.Transitions[0].EventType);
    }

    [Fact]
    public void DuringAny_ShouldReturnExistingStateWithNullName_WhenAlreadyExists()
    {
        // Arrange
        var sagaDescriptor = new SagaDescriptor<TestState>(s_context);
        var existingState = sagaDescriptor.DuringAny();

        // Act
        var retrievedState = sagaDescriptor.DuringAny();

        // Assert
        Assert.Same(existingState, retrievedState);
    }

    [Fact]
    public void CreateDefinition_ShouldReturnAllStatesInDescriptor()
    {
        // Arrange
        var sagaDescriptor = new SagaDescriptor<TestState>(s_context);
        sagaDescriptor.Initially();
        sagaDescriptor.During("TestState");
        sagaDescriptor.DuringAny();

        // Act
        var definition = sagaDescriptor.CreateConfiguration();

        // Assert
        Assert.Equal(2, definition.States.Count);
        Assert.NotNull(definition.DuringAny);
    }

    public sealed class TestState(Guid id, string state) : SagaStateBase(id, state);
}
