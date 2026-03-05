namespace Mocha.Sagas.Tests;

public class SagaStateDescriptorTests
{
    private static readonly IMessagingConfigurationContext s_context = TestMessagingSetupContext.Instance;

    [Fact]
    public void OnEvent_ShouldAddNewTransition_WhenEventTypeDoesNotExist()
    {
        // Arrange
        var stateDescriptor = new SagaStateDescriptor<TestState>(s_context, "TestState");

        // Act
        stateDescriptor.OnEvent<string>();

        // Assert
        var definition = stateDescriptor.CreateConfiguration();
        Assert.Equal(typeof(string), definition.Transitions[0].EventType);
    }

    [Fact]
    public void OnEvent_ShouldReturnExistingTransition_WhenEventTypeAlreadyExists()
    {
        // Arrange
        var stateDescriptor = new SagaStateDescriptor<TestState>(s_context, "TestState");
        var existingTransition = stateDescriptor.OnEvent<string>();

        // Act
        var retrievedTransition = stateDescriptor.OnEvent<string>();

        // Assert
        Assert.Same(existingTransition, retrievedTransition);
    }

    [Fact]
    public void CreateDefinition_ShouldReturnStateDefinitionWithTransitions()
    {
        // Arrange
        var stateDescriptor = new SagaStateDescriptor<TestState>(s_context, "TestState");
        stateDescriptor.OnEvent<string>();
        stateDescriptor.OnEvent<int>();

        // Act
        var definition = stateDescriptor.CreateConfiguration();

        // Assert
        Assert.NotNull(definition);
        Assert.Equal(2, definition.Transitions.Count);
    }

    public sealed class TestState(Guid id, string state) : SagaStateBase(id, state);
}
