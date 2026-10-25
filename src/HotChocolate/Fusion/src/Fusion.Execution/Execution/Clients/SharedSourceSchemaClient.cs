namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// A source schema client that all requests share until it expires or is invalidated.
/// The requests that use it never dispose it.
/// </summary>
internal sealed class SharedSourceSchemaClient(ISourceSchemaClient client, long expiresAt)
{
    private volatile bool _isInvalidated;

    /// <summary>
    /// Gets the shared client.
    /// </summary>
    public ISourceSchemaClient Client { get; } = client;

    /// <summary>
    /// Gets whether a request that starts at the given <see cref="System.Diagnostics.Stopwatch"/>
    /// timestamp may use the client.
    /// </summary>
    public bool IsValid(long timestamp) => !_isInvalidated && timestamp < expiresAt;

    /// <summary>
    /// Marks the client as unusable for requests that have not taken it yet.
    /// </summary>
    public void Invalidate() => _isInvalidated = true;
}
