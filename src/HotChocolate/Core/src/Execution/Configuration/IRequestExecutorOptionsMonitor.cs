namespace HotChocolate.Execution.Configuration;

/// <summary>
/// Used for notifications when <see cref="RequestExecutorSetup"/> instances change.
/// </summary>
public interface IRequestExecutorOptionsMonitor
{
    /// <summary>
    /// Returns a configured <see cref="RequestExecutorSetup"/>
    /// instance with the given name.
    /// </summary>
    ValueTask<RequestExecutorSetup> GetAsync(
        string schemaName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Registers a listener to be called whenever a named
    /// <see cref="RequestExecutorSetup"/> changes.
    /// </summary>
    /// <param name="listener">
    /// The action to be invoked when <see cref="RequestExecutorSetup"/> has changed.
    /// </param>
    /// <returns>
    /// An <see cref="IDisposable"/> which should be disposed to stop listening for changes.
    /// </returns>
    IDisposable OnChange(Action<string> listener);
}
