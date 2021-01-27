using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.Subscriptions
{
    public class SocketOperation : IAsyncDisposable
    {
        private readonly ISocketOperationManager _manager;
        private readonly Channel<JsonDocument> _channel;
        private bool _disposed;


        public string Id { get; }

        public SocketOperation(ISocketOperationManager manager)
            : this(manager, Guid.NewGuid().ToString())
        {
        }

        public SocketOperation(
            ISocketOperationManager manager,
            string id)
        {
            _manager = manager;
            Id = id;
            _channel = Channel.CreateUnbounded<JsonDocument>();
        }

        public async IAsyncEnumerable<JsonDocument> ReadAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                yield break;
            }

            ChannelReader<JsonDocument> reader = _channel.Reader;

            while (!_disposed && !reader.Completion.IsCompleted)
            {
                if (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    yield return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async ValueTask ReceiveAsync(
            JsonDocument message,
            CancellationToken cancellationToken)
        {
            await _channel.Writer.WriteAsync(message, cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _channel.Writer.TryComplete();

                await _manager.StopOperationAsync(Id, CancellationToken.None).ConfigureAwait(false);

                _disposed = true;
            }
        }
    }
}
