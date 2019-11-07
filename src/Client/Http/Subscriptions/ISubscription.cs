using System;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    public interface ISubscription
        : IDisposable
    {
        event EventHandler Disposed;

        string Id { get; }

        IResultParser ResultParser { get; }

        Task OnReceiveResultAsync(DataResultMessage message, CancellationToken cancellationToken);

        Task OnCompletedAsync(CancellationToken cancellationToken);
    }
}
