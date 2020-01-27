using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing
{
    public interface ISessionMessageReceiver<TMessage>
        where TMessage : ISessionMessage
    {
        ValueTask<IAsyncEnumerable<TMessage>> SubscribeAsync(
            string sessionId,
            CancellationToken cancellationToken = default);
    }
}
