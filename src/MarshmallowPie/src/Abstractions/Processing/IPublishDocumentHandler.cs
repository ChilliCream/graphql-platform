using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing
{
    public interface IPublishDocumentHandler
    {
        DocumentType Type { get; }

        Task HandleAsync(PublishDocumentMessage message, CancellationToken cancellationToken);
    }
}
