using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// A implementation of <see cref="ISocketProtocol"/> that uses graphql-ws protocol to
    /// communicate with the server
    /// </summary>
    public sealed class GraphQLWebSocketProtocol : SocketProtocolBase
    {
        private readonly ISocketClient _socketClient;
        private readonly MessagePipeline _receiver;
        private readonly SynchronizedMessageWriter _sender;

        /// <summary>
        /// Initializes a new instance of <see cref="GraphQLWebSocketProtocol"/>
        /// </summary>
        /// <param name="socketClient">
        /// The client where this protocol is using
        /// </param>
        public GraphQLWebSocketProtocol(ISocketClient socketClient)
        {
            _socketClient = socketClient ?? throw new ArgumentNullException(nameof(socketClient));
            _receiver = new MessagePipeline(socketClient, ProcessAsync);
            _sender = new SynchronizedMessageWriter(socketClient);
        }

        /// <inheritdoc />
        public override async Task StartOperationAsync(
            string operationId,
            OperationRequest request,
            CancellationToken cancellationToken)
        {
            if (_socketClient.IsClosed)
            {
                throw ThrowHelper.Protocol_CannotStartOperationOnClosedSocket(operationId);
            }

            await _sender
                .CommitAsync(
                    x => x.WriteStartOperationMessage(operationId, request),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task StopOperationAsync(
            string operationId,
            CancellationToken cancellationToken)
        {
            if (_socketClient.IsClosed)
            {
                return;
            }

            await _sender
                .CommitAsync(
                    x => x.WriteStopOperationMessage(operationId),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (_socketClient.IsClosed)
            {
                throw ThrowHelper.Protocol_CannotInitializeOnClosedSocket();
            }

            await _sender
                .CommitAsync(
                    x => x.WriteInitializeMessage(),
                    cancellationToken)
                .ConfigureAwait(false);

            _receiver.Start();
        }

        /// <inheritdoc />
        public override async Task TerminateAsync(CancellationToken cancellationToken)
        {
            if (_socketClient.IsClosed)
            {
                return;
            }

            await _sender
                .CommitAsync(
                    x => x.WriteTerminateMessage(),
                    cancellationToken)
                .ConfigureAwait(false);

            await _receiver.Stop().ConfigureAwait(false);
        }


        private ValueTask ProcessAsync(
            ReadOnlySequence<byte> slice,
            CancellationToken cancellationToken)
        {
            try
            {
                GraphQLWebSocketMessage message = GraphQLWebSocketMessageParser.Parse(slice);

                if (message.Id is { } id)
                {
                    switch (message.Type)
                    {
                        case GraphQLWebSocketMessageType.Data:
                            return Notify(
                                id,
                                // TODO: This copies the data into an array. This is needed because the read only sequence would be
                                // out of data once it arrives in the socket operation.
                                // We would need to pass the content serializer into the protcol to avoid this
                                new DataDocumentOperationMessage(message.Payload.ToArray()),
                                cancellationToken);
                        case GraphQLWebSocketMessageType.Complete:
                            return Notify(
                                id,
                                CompleteOperationMessage.Default,
                                cancellationToken);
                        case GraphQLWebSocketMessageType.Error:
                            return Notify(
                                id,
                                ErrorOperationMessage.UnexpectedServerError,
                                cancellationToken);
                        case GraphQLWebSocketMessageType.ConnectionError:
                            return Notify(
                                id,
                                ErrorOperationMessage.ConnectionError,
                                cancellationToken);
                        default:
                            return CloseSocketOnProtocolError(
                                "Invalid message type received: " + message.Type,
                                cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                return CloseSocketOnProtocolError(
                    "Invalid message received: " + ex.Message,
                    cancellationToken);
            }

            return default;
        }

        private async ValueTask CloseSocketOnProtocolError(
            string message,
            CancellationToken cancellationToken)
        {
            await _socketClient.CloseAsync(message,
                    SocketCloseStatus.ProtocolError,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async ValueTask DisposeAsync()
        {
            await _sender.DisposeAsync().ConfigureAwait(false);
            await _receiver.DisposeAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
