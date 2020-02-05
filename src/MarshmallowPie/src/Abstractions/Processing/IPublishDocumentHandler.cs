using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing
{
    public interface IPublishDocumentHandler
    {
        ValueTask<bool> CanHandleAsync(
            PublishDocumentMessage message,
            CancellationToken cancellationToken);

        Task HandleAsync(
            PublishDocumentMessage message,
            CancellationToken cancellationToken);
    }
}
