using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Server
{
    public interface ISocketConnection
        : IDisposable
    {
        bool Closed { get; }

        Task OpenAsync();

        Task SendAsync(
            ReadOnlySpan<byte> message,
            CancellationToken cancellationToken);

        Task ReceiveAsync(
            IBufferWriter<byte> writer,
            CancellationToken cancellationToken);

        Task CloseAsync(
            string message,
            CancellationToken cancellationToken);
    }
}
