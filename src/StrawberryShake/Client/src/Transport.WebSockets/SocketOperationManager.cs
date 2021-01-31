using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.Subscriptions
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
        /// <param name="socketProtocol"></param>
        public SocketOperationManager(ISocketProtocol socketProtocol)
        {
            _socketProtocol = socketProtocol ??
                throw new ArgumentNullException(nameof(socketProtocol));

            _socketProtocol.Subscribe(ReceiveMessage);
        }

        /// <inheritdoc />
        public Task<SocketOperation> StartOperationAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return StartOperationAsyncInternal(request, cancellationToken);
        }


        private async Task<SocketOperation> StartOperationAsyncInternal(
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
                // TODO: I guess this should not happen?
                throw new InvalidOperationException();
            }

            return operation;
        }

        /// <inheritdoc />
        public async Task StopOperationAsync(
            string operationId,
            CancellationToken cancellationToken = default)
        {
            if (_operations.TryRemove(operationId, out _))
            {
                await _socketProtocol
                    .StopOperationAsync(operationId, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Receive a message from the socket
        /// </summary>
        /// <param name="operationId">Id of the operation</param>
        /// <param name="payload">The payload of the message</param>
        /// <param name="cancellationToken">
        /// The cancellation token to cancel
        /// </param>
        private async ValueTask ReceiveMessage(
            string operationId,
            JsonDocument payload,
            CancellationToken cancellationToken = default)
        {
            if (_operations.TryGetValue(operationId, out var operation))
            {
                await operation.ReceiveAsync(payload, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (!_disposed && _operations.Count > 0)
            {
                _disposed = true;
                SocketOperation[] operations = _operations.Values.ToArray();

                for (var i = 0; i < operations.Length; i++)
                {
                    await operations[i].DisposeAsync();
                }

                _operations.Clear();
                _socketProtocol.Unsubscribe(ReceiveMessage);
            }
        }
    }
}
