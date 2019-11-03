using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Subscriptions
{
    public interface IMessagePipeline
    {
        Task ProcessAsync(
            ISocketConnection connection,
            ReadOnlySequence<byte> slice,
            CancellationToken cancellationToken);
    }
}
