using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions;
using StrawberryShake.Http.Subscriptions.Messages;
using StrawberryShake.Transport.Http;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.Subscriptions
{
    public sealed class SocketOperationManager : ISocketOperationManager
    {
        private readonly ISocketConnection _socketConnection;
        private readonly JsonOperationRequestSerializer _serializer = new();
        private readonly ConcurrentDictionary<string, SocketOperation> _operations = new();
        private readonly MessagePipelineHandler _handler;

        private bool _disposed;

        public SocketOperationManager(ISocketConnection socketConnection)
        {
            _socketConnection = socketConnection ??
                throw new ArgumentNullException(nameof(socketConnection));
            // TODO: This is wrong here. This way the user has no control over the pipeline and
            // cannot change it
            _handler = new MessagePipelineHandler(this);
            _handler.OnConnectAsync(socketConnection);
        }

        public async ValueTask ReceiveMessage(
            OperationMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message.Id is not null &&
                _operations.TryGetValue(message.Id, out var operation))
            {
                await operation.ReceiveAsync(message, cancellationToken);
            }
        }

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
                _socketConnection.Disposed += (sender, args) => StopOperationAsync(operation.Id);

                if (_socketConnection.IsClosed)
                {
                    _operations.TryRemove(operation.Id, out _);

                    // TODO: I think this should lead to an error?
                    throw new InvalidOperationException();
                    //return;
                }

                var writer = new SocketMessageWriter();

                writer.WriteStartObject();
                writer.WriteType(MessageTypes.Subscription.Start);
                writer.WriteId(operation.Id);
                writer.WriteStartPayload();
                _serializer.Serialize(request, writer);
                writer.WriteEndObject();

                await _socketConnection
                    .SendAsync(writer.Body, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                // TODO: I guess this should not happen?
                throw new InvalidOperationException();
            }

            return operation;
        }

        public async Task StopOperationAsync(
            string operationId,
            CancellationToken cancellationToken = default)
        {
            if (_operations.TryRemove(operationId, out _))
            {
                if (_socketConnection.IsClosed)
                {
                    return;
                }

                var writer = new SocketMessageWriter();

                writer.WriteStartObject();
                writer.WriteType(MessageTypes.Subscription.Stop);
                writer.WriteId(operationId);
                writer.WriteEndObject();

                await _socketConnection.SendAsync(writer.Body, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed && _operations.Count > 0)
            {
                _disposed = true;
                await _handler.OnDisconnectAsync(_socketConnection);
                SocketOperation[] operations = _operations.Values.ToArray();

                for (var i = 0; i < operations.Length; i++)
                {
                    await operations[i].DisposeAsync();
                }

                _operations.Clear();
                _disposed = true;
            }
        }
    }
}
