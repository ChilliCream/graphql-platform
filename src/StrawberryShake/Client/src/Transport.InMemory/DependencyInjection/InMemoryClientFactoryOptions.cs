namespace StrawberryShake.Transport.InMemory;

/// <summary>
/// Configure a <see cref="IInMemoryClient"/>
/// </summary>
/// <param name="client">The client to configure</param>
/// <param name="cancellationToken">
/// The cancellation token to cancel the configuration
/// </param>
public delegate ValueTask ConfigureInMemoryClientAsync(
    IInMemoryClient client,
    CancellationToken cancellationToken);

/// <summary>
/// Options of a <see cref="IInMemoryClient"/>
/// </summary>
public class InMemoryClientFactoryOptions
{
    /// <summary>
    /// Gets a list of operations used to configure an <see cref="IInMemoryClient"/>.
    /// </summary>
    public IList<ConfigureInMemoryClientAsync> InMemoryClientActions { get; } =
        new List<ConfigureInMemoryClientAsync>();
}
