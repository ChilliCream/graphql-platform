using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Subscriptions
{
    public interface ISocketSession
        : IDisposable
    {
        Task HandleAsync(CancellationToken cancellationToken);
    }
}
