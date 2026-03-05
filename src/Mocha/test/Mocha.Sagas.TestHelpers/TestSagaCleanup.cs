namespace Mocha.Sagas.Tests;

public class TestSagaCleanup : ISagaCleanup
{
    public List<SagaStateBase> CleanedStates { get; } = [];

    public Task CleanupAsync(Saga saga, SagaStateBase state, CancellationToken cancellationToken)
    {
        CleanedStates.Add(state);
        return Task.CompletedTask;
    }
}
