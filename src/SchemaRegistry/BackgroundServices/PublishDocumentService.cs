using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MarshmallowPie.Processing;

namespace BackgroundServices
{
    public class PublishDocumentService : BackgroundService
    {
        private readonly IMessageReceiver<PublishDocumentMessage> _messageReceiver;
        private readonly IPublishDocumentHandler[] _publishDocumentHandlers;

        public PublishDocumentService(
            IMessageReceiver<PublishDocumentMessage> messageReceiver,
            IEnumerable<IPublishDocumentHandler> publishDocumentHandlers)
        {
            _messageReceiver = messageReceiver;
            _publishDocumentHandlers = publishDocumentHandlers.ToArray();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IAsyncEnumerable<PublishDocumentMessage> eventStream =
                await _messageReceiver.SubscribeAsync(stoppingToken).ConfigureAwait(false);

            await foreach (PublishDocumentMessage message in
                eventStream.WithCancellation(stoppingToken))
            {
                foreach (IPublishDocumentHandler handler in _publishDocumentHandlers)
                {
                    if (handler.Type == message.Type)
                    {
                        await handler.HandleAsync(message, stoppingToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
