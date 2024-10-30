namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Represents a pool of <see cref="ISession"/>
/// </summary>
public interface ISessionPool
    : IAsyncDisposable
{
    /// <summary>
    /// Rents a named <see cref="ISession"/> from the pool.
    /// </summary>
    /// <param name="name">The name of the client</param>
    /// <param name="cancellationToken">The cancellation token for the operation</param>
    /// <returns>A socket session</returns>
    Task<ISession> CreateAsync(
        string name,
        CancellationToken cancellationToken = default);
}
