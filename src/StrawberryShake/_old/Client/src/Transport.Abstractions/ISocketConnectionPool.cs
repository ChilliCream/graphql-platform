using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport
{
    public interface ISocketConnectionPool
        : IDisposable
    {
        Task<ISocketConnection> RentAsync(
            string name,
            CancellationToken cancellationToken = default);

        Task ReturnAsync(
            ISocketConnection connection,
            CancellationToken cancellationToken = default);
    }
}
