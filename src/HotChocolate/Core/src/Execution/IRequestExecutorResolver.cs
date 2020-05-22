using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IRequestExecutorResolver
    {
        event EventHandler<RequestExecutorEvictedEventArgs>? RequestExecutorEvicted;

        ValueTask<IRequestExecutor> GetRequestExecutorAsync(
            string? name = null,
            CancellationToken cancellationToken = default);

        void EvictRequestExecutor(string? name = null);
    }
}