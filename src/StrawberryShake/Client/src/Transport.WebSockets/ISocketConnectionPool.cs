using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport
{
    public interface ISocketClientPool
        : IAsyncDisposable
    {
        Task<ISocketClient> RentAsync(
            string name,
            CancellationToken cancellationToken = default);

        Task ReturnAsync(
            ISocketClient client,
            CancellationToken cancellationToken = default);
    }
}
