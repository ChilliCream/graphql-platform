using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Messaging;

namespace MarshmallowPie.Messaging
{
    public interface IPublishDocumentHandler
    {
        DocumentType Type { get; }

        Task HandleAsync(PublishDocumentMessage message, CancellationToken cancellationToken);
    }
}
