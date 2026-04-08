using Mocha.Sagas;

namespace Mocha.Tests;

public class SagaStateBaseTests
{
    [Fact]
    public void SagaStateBase_DefaultConstructor_Should_InitializeWithDefaultValues_When_Created()
    {
        // act
        var state = new SagaStateBase();

        // assert
        Assert.NotEqual(Guid.Empty, state.Id);
        Assert.Equal("__Initial", state.State);
        Assert.NotNull(state.Errors);
        Assert.Empty(state.Errors);
        Assert.NotNull(state.Metadata);
    }

    [Fact]
    public void SagaStateBase_ParameterizedConstructor_Should_SetIdAndState_When_ParametersProvided()
    {
        // arrange
        var id = Guid.NewGuid();
        const string stateName = "Processing";

        // act
        var state = new SagaStateBase(id, stateName);

        // assert
        Assert.Equal(id, state.Id);
        Assert.Equal(stateName, state.State);
    }

    [Fact]
    public void SagaStateBase_Metadata_Should_BeInitializedAsHeaders_When_Created()
    {
        // arrange & act
        var state = new SagaStateBase();

        // assert
        Assert.NotNull(state.Metadata);
        Assert.IsAssignableFrom<IHeaders>(state.Metadata);
    }

    [Fact]
    public void SagaStateBase_Errors_Should_BeInitializedAsEmptyList_When_Created()
    {
        // arrange & act
        var state = new SagaStateBase();

        // assert
        Assert.NotNull(state.Errors);
        Assert.Empty(state.Errors);
    }

    [Fact]
    public void SagaStateBase_Errors_Should_BeModifiable_When_AddingErrors()
    {
        // arrange
        var state = new SagaStateBase();
        var error = new SagaError("Processing", "Error occurred");

        // act
        state.Errors.Add(error);

        // assert
        Assert.Single(state.Errors);
        Assert.Equal("Processing", state.Errors[0].CurrentState);
        Assert.Equal("Error occurred", state.Errors[0].Message);
    }
}
