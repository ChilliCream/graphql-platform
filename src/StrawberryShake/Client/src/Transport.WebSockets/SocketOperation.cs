using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.Subscriptions
{
    /// <summary>
    /// Represents a operation on a socket
    /// </summary>
    public class SocketOperation : ISocketOperation
    {
        private readonly ISocketOperationManager _manager;
        private readonly Channel<JsonDocument> _channel;
        private bool _disposed;

        /// <summary>
        /// The id of the operation
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Initializes a new <see cref="SocketOperation"/>
        /// </summary>
        /// <param name="manager">
        /// The socket operation manager that this operation manages
        /// </param>
        public SocketOperation(ISocketOperationManager manager)
            : this(manager, Guid.NewGuid().ToString())
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SocketOperation"/>
        /// </summary>
        /// <param name="manager">
        /// The socket operation manager that this operation manages
        /// </param>
        /// <param name="id">
        /// The id of this operation
        /// </param>
        public SocketOperation(
            ISocketOperationManager manager,
            string id)
        {
            _manager = manager;
            Id = id;
            _channel = Channel.CreateUnbounded<JsonDocument>();
        }

        /// <inheritdoc />
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
            await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
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
