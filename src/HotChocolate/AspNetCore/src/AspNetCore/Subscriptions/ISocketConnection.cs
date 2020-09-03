using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public interface ISocketConnection : IDisposable
    {
        HttpContext HttpContext { get; }

        bool Closed { get; }

        ISubscriptionManager Subscriptions { get; }

        IServiceProvider RequestServices { get; }

        CancellationToken RequestAborted { get; }

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
