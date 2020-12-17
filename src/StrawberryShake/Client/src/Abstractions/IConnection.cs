using System.Collections.Generic;
using System.Threading;

namespace StrawberryShake
{
    public interface IConnection<out TData>
    {
        IAsyncEnumerable<TData> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default);
    }
}
