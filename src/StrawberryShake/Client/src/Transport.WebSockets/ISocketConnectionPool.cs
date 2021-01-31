using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport
{
    /// <summary>
    /// Represents a pool of <see cref="ISocketClient"/>
    /// </summary>
    public interface ISocketClientPool
        : IAsyncDisposable
    {
        /// <summary>
        /// Rents a named <see cref="ISocketClient"/> from the pool.
        /// </summary>
        /// <param name="name">The name of the client</param>
        /// <param name="cancellationToken">The cancellation token for the operation</param>
        /// <returns>A socket client</returns>
        Task<ISocketClient> RentAsync(
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a socket client to the pool.
        /// </summary>
        /// <param name="client">The client</param>
        /// <param name="cancellationToken">The cancellation token for the operation</param>
        Task ReturnAsync(
            ISocketClient client,
            CancellationToken cancellationToken = default);
    }
}
