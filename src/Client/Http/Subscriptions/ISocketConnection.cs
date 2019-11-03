using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Subscriptions
{
    public interface ISocketConnection
        : IDisposable
    {
        bool Closed { get; }

        Task<bool> TryOpenAsync();

        Task SendAsync(
            ReadOnlyMemory<byte> message,
            CancellationToken cancellationToken);

        Task ReceiveAsync(
            PipeWriter writer,
            CancellationToken cancellationToken);

        Task CloseAsync(
            string message,
            SocketCloseStatus closeStatus,
            CancellationToken cancellationToken);
    }
}
