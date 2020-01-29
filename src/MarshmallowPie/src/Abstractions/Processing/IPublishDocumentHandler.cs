using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Processing;

namespace MarshmallowPie.Processing
{
    public interface IPublishDocumentHandler
    {
        DocumentType Type { get; }

        Task HandleAsync(PublishDocumentMessage message, CancellationToken cancellationToken);
    }
}
