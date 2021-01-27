using System.Collections.Concurrent;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions.Messages;
using StrawberryShake.Transport;

namespace StrawberryShake.Http.Subscriptions
{
    //TODO why do we need more than one pipeline?
    public class MessagePipelineHandler
    {
        private readonly ConcurrentDictionary<ISocketClient, MessagePipeline>
            _pipelines = new();
        private readonly DataResultMessageHandler _resultMessageHandler;

        public MessagePipelineHandler(ISocketProtocol protocol)
        {
            _resultMessageHandler = new DataResultMessageHandler(protocol);
        }

        public Task OnConnectAsync(ISocketClient client)
        {
            _pipelines.GetOrAdd(client,
                c =>
                {
                    var pipeline = new MessagePipeline(
                        client,
                        new[]
                        {
                            _resultMessageHandler
                        });
                    pipeline.Start();
                    return pipeline;
                });
            return Task.CompletedTask;
        }

        public async Task OnDisconnectAsync(ISocketClient client)
        {
            if (_pipelines.TryRemove(client, out MessagePipeline? pipeline))
            {
                await pipeline.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
