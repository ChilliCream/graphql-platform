using System.Collections.Concurrent;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions.Messages;
using StrawberryShake.Transport;
using StrawberryShake.Transport.Subscriptions;

namespace StrawberryShake.Http.Subscriptions
{
    public class MessagePipelineHandler
        : ISocketConnectionInterceptor
    {
        private readonly ConcurrentDictionary<ISocketConnection, MessagePipeline>
            _pipelines = new();
        private readonly DataResultMessageHandler _resultMessageHandler;

        public MessagePipelineHandler(ISocketOperationManager subscriptionManager)
        {
            _resultMessageHandler = new DataResultMessageHandler(subscriptionManager);
        }

        public Task OnConnectAsync(ISocketConnection connection)
        {
            _pipelines.GetOrAdd(connection,
                c =>
                {
                    var pipeline = new MessagePipeline(
                        connection,
                        new[]
                        {
                            _resultMessageHandler
                        });
                    pipeline.Start();
                    return pipeline;
                });
            return Task.CompletedTask;
        }

        public async Task OnDisconnectAsync(ISocketConnection connection)
        {
            if (_pipelines.TryRemove(connection, out MessagePipeline? pipeline))
            {
                await pipeline.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
