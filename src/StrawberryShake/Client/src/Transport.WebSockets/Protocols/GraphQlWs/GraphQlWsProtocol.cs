using System;
using System.Buffers;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// A implementation of <see cref="ISocketProtocol"/> that uses graphql-ws protocol to
    /// communicate with the server
    /// </summary>
    public sealed class GraphQlWsProtocol : SocketProtocolBase
    {
        private readonly ISocketClient _socketClient;
        private readonly MessagePipeline _receiver;
        private readonly SynchronizedMessageWriter _sender;

        /// <summary>
        /// Initializes a new instance of <see cref="GraphQlWsProtocol"/>
        /// </summary>
        /// <param name="socketClient">
        /// The client where this protocol is using
        /// </param>
        public GraphQlWsProtocol(ISocketClient socketClient)
        {
            _socketClient = socketClient;
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
                GraphQlWsMessage message = GraphQlWsMessageParser.Parse(slice);

                switch (message.Type)
                {
                    case GraphQlWsMessageTypes.Operation.Data when message is { Id : { } messageId }:
                        var reader = new Utf8JsonReader(message.Payload);
                        return Notify(messageId,
                            JsonDocument.ParseValue(ref reader),
                            cancellationToken);
                }
            }
            catch (SerializationException ex)
            {
                // TODO: Not sure what we do now
            }

            return default;
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
