#if !ASPNETCLASSIC
using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal interface IWebSocket
        : IDisposable
    {
        bool Closed { get; }

        Task SendAsync(
            Stream messageStream,
            CancellationToken cancellationToken);

        Task ReceiveAsync(
            PipeWriter writer,
            CancellationToken cancellationToken);

        Task CloseAsync(
            string message,
            CancellationToken cancellationToken);
    }
}

#endif
