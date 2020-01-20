using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing
{
    public interface IMessageSender<TMessage>
    {
        Task SendAsync(TMessage message, CancellationToken cancellationToken);
    }
}
