using System;
using Mocha;

namespace Mocha.Sagas.Tests;

public class SagaValidationTests
{
    private readonly IMessagingSetupContext _context = TestMessagingSetupContext.Instance;

    [Fact]
    public void NoStates_Should_ThrowException()
    {
        // Arrange
        // Act
        var exception =
            Assert.Throws<SagaInitializationException>(() =>
            {
                var saga = Saga.Create<TestState>(_ => { });
                saga.Initialize(_context);
            });

        // Assert
        Assert.Equal("Saga has no states defined.", exception.Message);
    }

    [Fact]
    public void OnlyStartAndFinalStates_Should_PassValidation()
    {
        // Arrange
        // Act
        var saga =
            Saga.Create<TestState>(descriptor =>
            {
                descriptor
                    .Initially()
                    .OnEvent<StartEvent>()
                    .TransitionTo("Success")
                    .StateFactory(s => new TestState(Guid.NewGuid(), "Success"));
                descriptor.Finally("Success");
            });

        // Assert - should not throw
        saga.Initialize(_context);
    }

    [Fact]
    public void OnlyStartState_Should_ThrowException()
    {
        // Arrange
        // Act
        var exception =
            Assert.Throws<SagaInitializationException>(() =>
            {
                var saga =
                    Saga.Create<TestState>(descriptor =>
                    {
                        descriptor
                            .Initially()
                            .OnEvent<StartEvent>()
                            .TransitionTo("Success")
                            .StateFactory(s => new TestState(Guid.NewGuid(), "Success"));
                        descriptor.During("Success").OnEvent<TriggerEvent>().TransitionTo("Success");
                    });
                saga.Initialize(_context);
            });

        // Assert
        Assert.Equal("No final states found in the saga.", exception.Message);
    }

    [Fact]
    public void OnlyFinalState_Should_ThrowException()
    {
        // Arrange
        // Act
        var exception =
            Assert.Throws<SagaInitializationException>(() =>
            {
                var saga =
                    Saga.Create<TestState>(descriptor =>
                        descriptor.Finally("Success"));
                saga.Initialize(_context);
            });

        // Assert
        Assert.Equal("No initial states found in the saga.", exception.Message);
    }

    [Fact]
    public void TransitionToUndefinedState_Should_ThrowException()
    {
        // Arrange
        // Act
        var exception =
            Assert.Throws<SagaInitializationException>(() =>
            {
                var saga =
                    Saga.Create<TestState>(descriptor =>
                    {
                        descriptor
                            .Initially()
                            .OnEvent<StartEvent>()
                            .TransitionTo("Undefined")
                            .StateFactory(s => new TestState(Guid.NewGuid(), "Started"));
                        descriptor.Finally("Success");
                    });
                saga.Initialize(_context);
            });

        // Assert
        Assert.Equal("State '__Initial' transitions to 'Undefined', which is not defined.", exception.Message);
    }

    [Fact]
    public void StateHasNoPathToFinalState_Should_ThrowException()
    {
        // Arrange
        // Act
        var exception =
            Assert.Throws<SagaInitializationException>(() =>
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
                        descriptor.During("Processing").OnEvent<TriggerEvent>().TransitionTo("Started");
                        descriptor.Finally("Success");
                    });
                saga.Initialize(_context);
            });

        // Assert
        Assert.Equal(
            "The following states cannot reach a final state: __Initial, Started, Processing",
            exception.Message);
    }

    [Fact]
    public void ValidPathAndCycle_Should_ThrowException()
    {
        // Arrange
        // Act
        var exception =
            Assert.Throws<SagaInitializationException>(() =>
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
                        descriptor.During("Processing").OnEvent<StartEvent>().TransitionTo("Cycle");
                        descriptor.During("Cycle").OnEvent<TriggerEvent>().TransitionTo("Cycle");
                        descriptor.Finally("Success");
                        descriptor.Finally("Cancelled");
                    });
                saga.Initialize(_context);
            });

        // Assert
        Assert.Equal("The following states cannot reach a final state: Cycle", exception.Message);
    }

    private class TestState(Guid id, string state) : SagaStateBase(id, state);

    private sealed class StartEvent;

    private sealed class TriggerEvent;

    private sealed class CancelEvent;

    private sealed record TestMessage(Guid Id);
}
