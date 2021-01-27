using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions;
using StrawberryShake.Transport.Http;

namespace StrawberryShake.Transport.WebSockets
{
    public class SubscriptionTransportWsProtocol : ISocketProtocol
    {
        private readonly JsonOperationRequestSerializer _serializer = new();
        private readonly MessagePipelineHandler _handler;
        private readonly HashSet<OnReceiveAsync> _listeners = new();

        private ISocketClient? _socketClient;
        private bool _disposed;

        public SubscriptionTransportWsProtocol()
        {
            _handler = new MessagePipelineHandler(this);
        }

        public string ProtocolName => "graphql-ws";

        public event EventHandler Disposed = default!;

        public async Task StartOperationAsync(
            string operationId,
            OperationRequest request,
            CancellationToken cancellationToken)
        {
            EnsureInitialized(out ISocketClient client);

            if (client.IsClosed)
            {
                //TODO: client is closed
                throw new InvalidOperationException();
            }

            var writer = new SocketMessageWriter();

            writer.WriteStartObject();
            writer.WriteType(MessageTypes.Subscription.Start);
            writer.WriteId(operationId);
            writer.WriteStartPayload();
            _serializer.Serialize(request, writer);
            writer.WriteEndObject();

            await client
                .SendAsync(writer.Body, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task StopOperationAsync(
            string operationId,
            CancellationToken cancellationToken)
        {
            EnsureInitialized(out ISocketClient client);

            if (client.IsClosed)
            {
                return;
            }

            var writer = new SocketMessageWriter();

            writer.WriteStartObject();
            writer.WriteType(MessageTypes.Subscription.Stop);
            writer.WriteId(operationId);
            writer.WriteEndObject();

            await client.SendAsync(writer.Body, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task InitializeAsync(
            ISocketClient socketClient,
            CancellationToken cancellationToken)
        {
            _socketClient = socketClient;
            var messageWriter = new SocketMessageWriter();
            messageWriter.WriteStartObject();
            messageWriter.WriteType(MessageTypes.Connection.Initialize);
            messageWriter.WriteEndObject();

            await _socketClient
                .SendAsync(messageWriter.Body, cancellationToken)
                .ConfigureAwait(false);

            await _handler.OnConnectAsync(socketClient).ConfigureAwait(false);
        }

        public async Task TerminateAsync(CancellationToken cancellationToken)
        {
            EnsureInitialized(out ISocketClient client);

            var messageWriter = new SocketMessageWriter();
            messageWriter.WriteStartObject();
            messageWriter.WriteType(MessageTypes.Connection.Terminate);
            messageWriter.WriteEndObject();

            await _handler.OnDisconnectAsync(client).ConfigureAwait(false);

            await client.SendAsync(messageWriter.Body, cancellationToken).ConfigureAwait(false);

            _socketClient = null;
        }

        public void Subscribe(OnReceiveAsync listener)
        {
            _listeners.Add(listener);
        }

        public void Unsubscribe(OnReceiveAsync listener)
        {
            _listeners.Remove(listener);
        }

        public async ValueTask Notify(
            string messageId,
            JsonDocument document,
            CancellationToken cancellationToken)
        {
            foreach (var listener in _listeners)
            {
                await listener(messageId, document, cancellationToken).ConfigureAwait(false);
            }
        }

        private void EnsureInitialized([NotNull] out ISocketClient client)
        {
            client = _socketClient ?? throw new InvalidOperationException();
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                return;
            }

            _disposed = true;

            if (_socketClient is not null)
            {
                await _handler.OnDisconnectAsync(default!).ConfigureAwait(false);
            }

            Disposed.Invoke(this, EventArgs.Empty);
        }
    }
}
