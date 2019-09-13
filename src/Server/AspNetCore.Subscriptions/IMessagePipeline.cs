using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public interface IMessagePipeline
    {
        Task ProcessAsync(
            ISocketConnection connection,
            ReadOnlySequence<byte> slice,
            CancellationToken cancellationToken);
    }
}
