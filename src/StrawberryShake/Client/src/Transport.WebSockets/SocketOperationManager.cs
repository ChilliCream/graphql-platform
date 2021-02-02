using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets
{
    /// <inheritdoc />
    public sealed class SocketOperationManager : ISocketOperationManager
    {
        private readonly ISocketProtocol _socketProtocol;
        private readonly ConcurrentDictionary<string, SocketOperation> _operations = new();

        private bool _disposed;

        /// <summary>
        /// Creates a new instance <see cref="SocketOperationManager"/>
        /// </summary>
        public SocketOperationManager(ISocketClient socketClient)
        {
            if (socketClient is null)
            {
                throw new ArgumentNullException(nameof(socketClient));
            }

            socketClient.TryGetProtocol(out ISocketProtocol? socketProtocol);
            _socketProtocol = socketProtocol ??
                throw ThrowHelper.OperationManager_SocketWasNotInitialized(socketClient.Name);
            _socketProtocol.Subscribe(ReceiveMessage);
        }

        /// <inheritdoc />
        public Task<ISocketOperation> StartOperationAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return StartOperationAsyncInternal(request, cancellationToken);
        }


        private async Task<ISocketOperation> StartOperationAsyncInternal(
            OperationRequest request,
            CancellationToken cancellationToken)
        {
            var operation = new SocketOperation(this);
            if (_operations.TryAdd(operation.Id, operation))
            {
                try
                {
                    _socketProtocol.Disposed += (sender, args) => StopOperationAsync(operation.Id);

                    await _socketProtocol
                        .StartOperationAsync(operation.Id, request, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch
                {
                    _operations.TryRemove(operation.Id, out _);
                    throw;
                }
            }
            else
            {
                throw ThrowHelper.OperationManager_OperationWasAlreadyRegistered(operation.Id);
            }

            return operation;
        }

        /// <inheritdoc />
        public async Task StopOperationAsync(
            string operationId,
            CancellationToken cancellationToken = default)
        {
            if (_operations.TryRemove(operationId, out var operation))
            {
                await _socketProtocol
                    .StopOperationAsync(operationId, cancellationToken)
                    .ConfigureAwait(false);

                await operation.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Receive a message from the socket
        /// </summary>
        /// <param name="operationId">Id of the operation</param>
        /// <param name="message">The payload of the message</param>
        /// <param name="cancellationToken">
        /// The cancellation token to cancel
        /// </param>
        private async ValueTask ReceiveMessage(
            string operationId,
            OperationMessage message,
            CancellationToken cancellationToken = default)
        {
            if (_operations.TryGetValue(operationId, out SocketOperation? operation))
            {
                await operation.ReceiveMessageAsync(message, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_operations.Count > 0)
                {
                    SocketOperation[] operations = _operations.Values.ToArray();

                    for (var i = 0; i < operations.Length; i++)
                    {
                        await operations[i].DisposeAsync();
                    }

                    _operations.Clear();
                }

                _socketProtocol.Unsubscribe(ReceiveMessage);
            }
        }
    }
}
