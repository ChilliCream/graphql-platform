using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing
{
    public interface IMessageStream<TMessage>
        : IAsyncEnumerable<TMessage?>
        where TMessage : class
    {
        ValueTask CompleteAsync();
    }
}
