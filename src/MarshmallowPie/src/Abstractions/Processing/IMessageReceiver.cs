using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing
{
    public interface IMessageReceiver<TMessage>
        where TMessage : class
    {
        ValueTask<IMessageStream<TMessage>> SubscribeAsync(
            CancellationToken cancellationToken = default);
    }
}
