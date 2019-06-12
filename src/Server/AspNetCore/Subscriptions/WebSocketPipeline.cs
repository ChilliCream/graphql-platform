#if !ASPNETCLASSIC
using Newtonsoft.Json;
using System;
using System.Text;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class WebSocketPipeline
    {
        private static readonly IRequestHandler[] RequestHandlers =
            {
                new ConnectionInitializeHandler(),
                new ConnectionTerminateHandler(),
                new SubscriptionStartHandler(),
                new SubscriptionStopHandler(),
            };

        private readonly IWebSocketContext _context;
        private readonly CancellationTokenSource _cts;

        internal WebSocketPipeline(
            IWebSocketContext context,
            CancellationTokenSource cts)
        {
            _context = context;
            _cts = cts;
        }

        internal async Task ProcessMessageAsync(
            ReadOnlySequence<byte> slice,
            CancellationToken cancellationToken)
        {
            using (var combined = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _cts.Token))
            {
                string json = Encoding.UTF8.GetString(slice.ToArray());
                if (!string.IsNullOrEmpty(json.Trim()))
                {
                    await HandleMessageAsync(json, combined.Token)
                        .ConfigureAwait(false);
                }
                else
                {
                    await _context
                        .SendConnectionKeepAliveMessageAsync(combined.Token)
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task HandleMessageAsync(
            string content,
            CancellationToken cancellationToken)
        {
            GenericOperationMessage message = JsonConvert
                .DeserializeObject<GenericOperationMessage>(content);

            foreach (IRequestHandler handler in RequestHandlers)
            {
                if (handler.CanHandle(message))
                {
                    await handler.HandleAsync(
                            _context,
                            message,
                            cancellationToken)
                        .ConfigureAwait(false);

                    return;
                }
            }

            throw new NotSupportedException(
                "The specified message type is not supported.");
        }
    }
}

#endif
