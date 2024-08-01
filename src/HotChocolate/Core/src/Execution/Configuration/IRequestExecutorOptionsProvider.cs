namespace HotChocolate.Execution.Configuration;

/// <summary>
/// Provides dynamic configurations.
/// </summary>
public interface IRequestExecutorOptionsProvider
{
    /// <summary>
    /// Gets named configuration options.
    /// </summary>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/>.
    /// </param>
    /// <returns>
    /// Returns the configuration options of this provider.
    /// </returns>
    ValueTask<IEnumerable<IConfigureRequestExecutorSetup>> GetOptionsAsync(
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
    IDisposable OnChange(Action<IConfigureRequestExecutorSetup> listener);
}
