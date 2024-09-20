namespace StrawberryShake.Transport.InMemory;

/// <summary>
/// A factory abstraction for a component that can create <see cref="InMemoryClient"/>
/// instances with custom configuration for a given logical name.
/// </summary>
public interface IInMemoryClientFactory
{
    /// <summary>
    /// Creates and configures an <see cref="IInMemoryClient"/> instance using the
    /// configuration that corresponds to the logical name specified by <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The logical name of the client to create.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to cancel the creation of a client
    /// </param>
    /// <returns>A new <see cref="IInMemoryClient"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// Each call to <see cref="CreateAsync"/> is guaranteed to return
    /// a new <see cref="IInMemoryClient"/> instance. Callers may cache the returned
    /// <see cref="IInMemoryClient"/> instance indefinitely or surround its use in a
    /// <langword>using</langword> block to dispose it when desired.
    /// </para>
    /// <para>
    /// Callers are also free to mutate the returned <see cref="IInMemoryClient"/>
    /// instance's public properties as desired.
    /// </para>
    /// </remarks>
    ValueTask<IInMemoryClient> CreateAsync(
        string name,
        CancellationToken cancellationToken = default);
}
