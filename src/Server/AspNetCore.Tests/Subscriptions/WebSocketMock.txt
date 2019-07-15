using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class WebSocketMock
        : IWebSocket
    {
        public Queue<GenericOperationMessage> Incoming { get; } =
            new Queue<GenericOperationMessage>();

        public List<GenericOperationMessage> Outgoing { get; } =
            new List<GenericOperationMessage>();

        private bool _closed;

        public bool Closed => _closed;

        public Task SendAsync(
            Stream messageStream,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[messageStream.Length];

            messageStream.Read(buffer, 0, buffer.Length);

            string json = Encoding.UTF8.GetString(buffer);

            Outgoing.Add(JsonConvert
                .DeserializeObject<GenericOperationMessage>(json));

            return Task.CompletedTask;
        }

        public async Task ReceiveAsync(
            PipeWriter writer,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested
                   && !Incoming.Any())
            {
                await Task.Delay(100, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            GenericOperationMessage message = Incoming.Dequeue();
            string json = JsonConvert.SerializeObject(message);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            Memory<byte> memory = writer.GetMemory(buffer.Length);
            for (int i = 0; i < buffer.Length; i++)
            {
                memory.Span[i] = buffer[i];
            }

            writer.Advance(buffer.Length);

            await writer
                .FlushAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public Task CloseAsync(
            string message,
            CancellationToken cancellationToken)
        {
            _closed = true;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
