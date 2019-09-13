using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Server
{
    public interface ISocketSession
        : IDisposable
    {
        Task HandleAsync(CancellationToken cancellationToken);
    }
}
