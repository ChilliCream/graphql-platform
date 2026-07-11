namespace Mocha.Sagas;

/// <summary>
/// Interface for cleaning up a saga after it has been completed.
/// </summary>
// TODO this is still not wired up correctly!
internal interface ISagaCleanup
{
    Task CleanupAsync(Saga saga, SagaStateBase state, CancellationToken cancellationToken);
}
