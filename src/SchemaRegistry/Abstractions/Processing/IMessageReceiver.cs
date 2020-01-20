using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing
{
    public interface IMessageReceiver<TMessage>
    {
        Task<IAsyncEnumerable<TMessage>> SubscribeAsync(CancellationToken cancellationToken);
    }
}
