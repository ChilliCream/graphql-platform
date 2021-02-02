using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// Represents a pool of <see cref="ISessionManager"/>
    /// </summary>
    public interface ISocketSessionPool
        : IAsyncDisposable
    {
        /// <summary>
        /// Rents a named <see cref="ISessionManager"/> from the pool.
        /// </summary>
        /// <param name="name">The name of the client</param>
        /// <param name="cancellationToken">The cancellation token for the operation</param>
        /// <returns>A socket sessionManager</returns>
        Task<ISessionManager> RentAsync(
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a socket sessionManager to the pool.
        /// </summary>
        /// <param name="sessionManager">The sessionManager</param>
        /// <param name="cancellationToken">The cancellation token for the operation</param>
        Task ReturnAsync(
            ISessionManager sessionManager,
            CancellationToken cancellationToken = default);
    }
}
