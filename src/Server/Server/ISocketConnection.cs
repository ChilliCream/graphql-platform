using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Server
{
    public interface ISocketConnection
        : IDisposable
    {
        bool Closed { get; }

        ISubscriptionManager Subscriptions { get; }

        IServiceProvider RequestServices { get; }

        Task<bool> TryOpenAsync();

        Task SendAsync(
            byte[] message,
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
