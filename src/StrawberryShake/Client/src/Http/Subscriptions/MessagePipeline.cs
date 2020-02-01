using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport;

namespace StrawberryShake.Http.Subscriptions
{
    internal sealed class MessagePipeline
        : IMessagePipeline
    {
        private readonly Pipe _input = new Pipe();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly MessageReceiver _receiver;
        private readonly MessageProcessor _processor;

        public MessagePipeline(
            ISocketConnection connection,
            ISubscriptionManager subscriptionManager,
            IEnumerable<IMessageHandler> messageHandlers)
        {
            _receiver = new MessageReceiver(connection, _input.Writer);
            _processor = new MessageProcessor(
                connection,
                new MessageParser(subscriptionManager),
                messageHandlers,
                _input.Reader);
        }

        public void Start()
        {
            _receiver.Start(_cts.Token);
            _processor.Start(_cts.Token);
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();

            if (_processor.InnerTask is { })
            {
                await _processor.InnerTask.ConfigureAwait(false);
            }

            if (_receiver.InnerTask is { })
            {
                await _receiver.InnerTask.ConfigureAwait(false);
            }

            _cts.Dispose();
        }
    }
}
