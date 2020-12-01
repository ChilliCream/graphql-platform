using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport
{
    public interface ISocketConnection
        : IDisposable
    {
        event EventHandler Disposed;

        string Name { get; }

        bool IsClosed { get; }

        Task OpenAsync(
            CancellationToken cancellationToken = default);

        Task SendAsync(
            ReadOnlyMemory<byte> message,
            CancellationToken cancellationToken = default);

        Task ReceiveAsync(
            PipeWriter writer,
            CancellationToken cancellationToken = default);

        Task CloseAsync(
            string message,
            SocketCloseStatus closeStatus,
            CancellationToken cancellationToken = default);
    }
}
