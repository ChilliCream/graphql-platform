using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MarshmallowPie.Processing;

namespace MarshmallowPie.BackgroundServices
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
            IMessageStream<PublishDocumentMessage> eventStream =
                await _messageReceiver.SubscribeAsync(stoppingToken).ConfigureAwait(false);

            await foreach (PublishDocumentMessage? message in
                eventStream.WithCancellation(stoppingToken))
            {
                if (message is null)
                {
                    break;
                }

                try
                {
                    foreach (IPublishDocumentHandler handler in _publishDocumentHandlers)
                    {
                        if (await handler.CanHandleAsync(message, stoppingToken).ConfigureAwait(false))
                        {
                            await handler.HandleAsync(message, stoppingToken).ConfigureAwait(false);
                            break;
                        }
                    }

                    await eventStream.CompleteAsync().ConfigureAwait(false);
                }
                catch { }
            }
        }
    }
}
