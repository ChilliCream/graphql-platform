#if !ASPNETCLASSIC
using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class SubscriptionReceiver
    {
        private readonly IWebSocketContext _webSocket;
        private readonly PipeWriter _writer;
        private readonly CancellationTokenSource _cts;

        public SubscriptionReceiver(
            IWebSocketContext webSocket,
            PipeWriter writer,
            CancellationTokenSource cts)
        {
            _webSocket = webSocket;
            _writer = writer;
            _cts = cts;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var combined = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _cts.Token))
            {
                while (!_webSocket.Closed || !combined.IsCancellationRequested)
                {
                    await _webSocket
                        .ReceiveMessageAsync(_writer, combined.Token)
                        .ConfigureAwait(false);

                    await WriteMessageDelimiterAsync(combined.Token)
                        .ConfigureAwait(false);
                }

                _writer.Complete();
            }
        }

        private async Task WriteMessageDelimiterAsync(
            CancellationToken cancellationToken)
        {
            Memory<byte> memory = _writer.GetMemory(1);

            memory.Span[0] = Subscription._delimiter;

            _writer.Advance(1);

            await _writer
                .FlushAsync(cancellationToken)
                .ConfigureAwait(false);

        }
    }
}

#endif
