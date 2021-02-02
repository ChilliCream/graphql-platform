using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// Represents a operation on a socket
    /// </summary>
    public sealed class SocketOperation : ISocketOperation
    {
        private readonly ISessionManager _manager;
        private readonly Channel<OperationMessage> _channel;
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
        public SocketOperation(ISessionManager manager)
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
            ISessionManager manager,
            string id)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            Id = id ?? throw new ArgumentNullException(nameof(id));
            _channel = Channel.CreateUnbounded<OperationMessage>();
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<OperationMessage> ReadAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                yield break;
            }

            ChannelReader<OperationMessage> reader = _channel.Reader;

            while (!_disposed && !reader.Completion.IsCompleted)
            {
                if (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    yield return await reader
                        .ReadAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        internal async ValueTask ReceiveMessageAsync(
            OperationMessage message,
            CancellationToken cancellationToken)
        {
            if (!_disposed)
            {
                await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
            }
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
