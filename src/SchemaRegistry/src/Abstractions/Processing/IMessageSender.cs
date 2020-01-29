using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing
{
    public interface IMessageSender<TMessage>
    {
        ValueTask SendAsync(TMessage message, CancellationToken cancellationToken = default);
    }
}
